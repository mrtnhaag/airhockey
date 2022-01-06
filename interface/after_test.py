import tkinter as tk
import cv2
import serial
from serial.serialutil import SerialException
import cv2
import numpy as np
from PIL import Image, ImageTk
import time
import onnx
import onnxruntime

class Window(tk.Tk):
    def __init__(self, master=None):
        super().__init__()
        self.master = master
        self.framerate = None
        #variables calib
        self.calibration_circles = None
        self.detected_circles_positions = {}
        self.simulated_circles_positions = {"TopLeftCircle": np.array([1.177, -3.046]),
                                            "BottomLeftCircle": np.array([-1.161, -3.046]),
                                            "TopRightCircle": np.array([1.189, 3.073]),
                                            "BottomRightCircle": np.array([-1.148, 3.091])}

        self.sim2real_rotation_matrix = None
        self.pusher_position_pixels = None
        self.pusher_position_sim = None
        self.pusher_rectangle = None
        #variables nn
        self.agent = onnxruntime.InferenceSession("opponent.onnx")
        #variables transform
        self.scale = None
        self.angle_deg = None
        self.angle_rad = None
        self.offset = None
        #rest variables
        self.puck_rectangle = None
        self.puck_found_time_step = 0
        self.videosource = None
        self.current_frame = None
        self.draw_frame = None
        self.serial_connection = None
        self.livedemo = tk.BooleanVar()
        self.caliVar = tk.BooleanVar()
        self.MoveVar = tk.IntVar()
        self.MoveVar.set(2)
        self.BaudChoice = [
            "3",
            "4",
            "8"
        ]
        self.BaudVar = tk.StringVar()
        self.StatusVar = tk.StringVar()
        self.StatusVar.set("start")
        self.BaudVar.set(self.BaudChoice[0])

        #gui elements
        self.CheckLive = tk.Checkbutton(self, text="live", variable=self.livedemo, command=self.change_vid_mode)
        self.Checkcalibration = tk.Checkbutton(self, text="calibration", variable=self.caliVar)
        self.Radiomove = tk.Radiobutton(self, text="Auto-Move", variable=self.MoveVar, value=1)
        self.RadioNOmove = tk.Radiobutton(self, text="Manual Move", variable=self.MoveVar, value=2)
        self.Menubau = tk.OptionMenu(self, self.BaudVar, *self.BaudChoice)
        self.EntryCOM = tk.Entry(self, text="port")
        self.LabelVideo = tk.Label(self)
        self.LabelStatus = tk.Label(self, textvariable=self.StatusVar)

        self.ButtonMoveleft = tk.Button(self, text="left", command=lambda: self.manualmove(1))
        self.ButtonMoveright = tk.Button(self, text="right", command=lambda: self.manualmove(2))
        self.ButtonMoveup = tk.Button(self, text="up", command=lambda: self.manualmove(3))
        self.ButtonMovedown = tk.Button(self, text="down", command=lambda: self.manualmove(4))
        self.ButtonMovestop = tk.Button(self, text="stop", command=lambda: self.manualmove(0))
        self.ButtonSerialStart = tk.Button(self, text="serial start", command=self.connect_via_serial)


        self.LabelVideo.place(x=200, y=10)
        self.LabelStatus.place(x=10, y=400)
        self.Radiomove.place(x=20, y=20)
        self.RadioNOmove.place(x=20, y=40)
        self.ButtonMoveleft.place(x=10, y=100)
        self.ButtonMoveright.place(x=90, y=100)
        self.ButtonMoveup.place(x=50, y=70)
        self.ButtonMovedown.place(x=50, y=130)
        self.ButtonMovestop.place(x=50, y=100)
        self.EntryCOM.place(x=10, y=200)
        self.Menubau.place(x=10, y=220)
        self.ButtonSerialStart.place(x=10, y=250)
        self.CheckLive.place(x=10, y=300)
        self.Checkcalibration.place(x=10, y=320)





        #self.LabelVideo.after(100, self.show_frames)
        self.show_frames()



    def calibration(self):
        cali_circ = self.find_calibration_circles(False)
        if cali_circ:
            self.StatusVar.set("calibration circles fond")
            cali_trans = self.calibrate_coordinate_transformation(False)
            if cali_trans:
                self.StatusVar.set("calibration finished")
                self.caliVar.set(False)
        else:
            self.StatusVar.set("calibration circles not fond")

    def play_agent(self):

        #outputs = self.agent.run(None, {'input': np.array([0,0,0,0,0,0,0,0])})
        #print(outputs)
        pass


    def calibrate_coordinate_transformation(self, show_images=False):
        # Match circles with table corners
        for c in self.calibration_circles[0, :]:
            # Circle Array: Center X, Center Y, Radius
            if 130 < c[0] < 180 and 150 < c[1] < 240:
                self.detected_circles_positions["TopLeftCircle"] = np.array([c[0], c[1]], dtype=int)
            elif 130 < c[0] < 180 and 300 < c[1] < 390:
                self.detected_circles_positions["BottomLeftCircle"] = np.array([c[0], c[1]], dtype=int)
            elif 530 < c[0] < 600 and 150 < c[1] < 240:
                self.detected_circles_positions["TopRightCircle"] = np.array([c[0], c[1]], dtype=int)
            elif 530 < c[0] < 600 and 300 < c[1] < 390:
                self.detected_circles_positions["BottomRightCircle"] = np.array([c[0], c[1]], dtype=int)
            else:
                print("INFO: Circle with Center at: ({}, {}) could not be matched".format(c[0], c[1]))
        print("INFO: Detected Circle Positions:", self.detected_circles_positions)

        if show_images:
            current_frame_copy = self.current_frame.copy()
            for key, val in self.detected_circles_positions.items():
                cv2.circle(current_frame_copy, val, 5, (255, 0, 0), -1)
                cv2.putText(current_frame_copy, key, val, cv2.FONT_HERSHEY_PLAIN, 1, (255, 0, 0))
                cv2.imshow("Circle Matching Image", current_frame_copy)

        # Calculate the difference in angle, scale and offset between image and simulation
        angle_list = []
        scale_list = []
        x_offset_list = []
        y_offset_list = []
        if "TopLeftCircle" in self.detected_circles_positions and "TopRightCircle" in self.detected_circles_positions:
            pixel_vector = self.detected_circles_positions["TopRightCircle"] - \
                          self.detected_circles_positions["TopLeftCircle"]
            simulation_vector = self.simulated_circles_positions["TopRightCircle"] - \
                                self.simulated_circles_positions["TopLeftCircle"]
            # Get the angle and scale between the two vectors
            angle_list.append(self.angle_between(pixel_vector, simulation_vector))
            scale_list.append(self.scale_between(pixel_vector, simulation_vector))
            # Calculate the x-coordinate origin offset
            x_offset_list.append(pixel_vector[0]/2 + self.detected_circles_positions["TopLeftCircle"][0])

        if "TopLeftCircle" in self.detected_circles_positions and "BottomLeftCircle" in self.detected_circles_positions:
            pixel_vector = self.detected_circles_positions["BottomLeftCircle"] - \
                          self.detected_circles_positions["TopLeftCircle"]
            simulation_vector = self.simulated_circles_positions["BottomLeftCircle"] - \
                                self.simulated_circles_positions["TopLeftCircle"]
            # Get the angle between the two vectors
            angle_list.append(self.angle_between(pixel_vector, simulation_vector))
            scale_list.append(self.scale_between(pixel_vector, simulation_vector))
            # Calculate the x-coordinate origin offset
            y_offset_list.append(pixel_vector[1]/2 + self.detected_circles_positions["TopLeftCircle"][1])

        if "BottomLeftCircle" in self.detected_circles_positions and "BottomRightCircle" in self.detected_circles_positions:
            pixel_vector = self.detected_circles_positions["BottomRightCircle"] - \
                          self.detected_circles_positions["BottomLeftCircle"]
            simulation_vector = self.simulated_circles_positions["BottomRightCircle"] - \
                                self.simulated_circles_positions["BottomLeftCircle"]
            # Get the angle between the two vectors
            angle_list.append(self.angle_between(pixel_vector, simulation_vector))
            scale_list.append(self.scale_between(pixel_vector, simulation_vector))
            # Calculate the x-coordinate origin offset
            x_offset_list.append(pixel_vector[0]/2 + self.detected_circles_positions["BottomLeftCircle"][0])

        if "BottomRightCircle" in self.detected_circles_positions and "TopRightCircle" in self.detected_circles_positions:
            pixel_vector = self.detected_circles_positions["BottomRightCircle"] - \
                          self.detected_circles_positions["TopRightCircle"]
            simulation_vector = self.simulated_circles_positions["BottomRightCircle"] - \
                                self.simulated_circles_positions["TopRightCircle"]
            # Get the angle between the two vectors
            angle_list.append(self.angle_between(pixel_vector, simulation_vector))
            scale_list.append(self.scale_between(pixel_vector, simulation_vector))
            # Calculate the x-coordinate origin offset
            y_offset_list.append(pixel_vector[1]/2 + self.detected_circles_positions["TopRightCircle"][1])

        # Average scale, angle and offset
        if len(scale_list):
            self.scale = np.mean(scale_list)
        if len(angle_list):
            self.angle_deg = -np.mean(angle_list)
            self.angle_rad = self.angle_deg*np.pi/180
        if len(y_offset_list and x_offset_list):
            self.offset = np.array((np.mean(x_offset_list), np.mean(y_offset_list)))

        if self.scale and self.angle_deg and np.all(self.offset):
            if 50 < self.scale < 70 and -80 > self.angle_deg > -100:
                print("INFO: Calibration successful! "
                      "Scale: {:.2f}, Angle: {:.2f}°, Offset: x: {:.1f} y:{:.1f}".format(self.scale, self.angle_deg,
                                                                                         self.offset[0], self.offset[1]))
            else:
                print("WARNING: Calibration might have failed! "
                      "Scale: {:.2f}, Angle: {:.2f}, Offset: x: {:.1f} y:{:.1f}".format(self.scale, self.angle_deg,
                                                                                        self.offset[0], self.offset[1]))


            # Calculate rotation matrix
            self.sim2real_rotation_matrix = np.array(((np.cos(self.angle_rad), -np.sin(self.angle_rad)),
                                                      (np.sin(self.angle_rad), np.cos(self.angle_rad))))

            if show_images:
                current_frame_copy = self.current_frame.copy()
                for key, val in self.detected_circles_positions.items():
                    cv2.circle(current_frame_copy, val, 5, (255, 0, 0), -1)
                    cv2.putText(current_frame_copy, key, val, cv2.FONT_HERSHEY_PLAIN, 1, (255, 0, 0))
                cv2.circle(current_frame_copy, (np.int(self.offset[0]), np.int(self.offset[1])), 5, (0, 0, 255), -1)
                cv2.putText(current_frame_copy, "Origin", (np.int(self.offset[0]), np.int(self.offset[1])),
                            cv2.FONT_HERSHEY_PLAIN, 1, (0, 0, 255))
                cv2.imshow("Circle Matching Image", current_frame_copy)
            return True
        else:
            return False

    def get_pusher_coordinates(self, show_images=False):

        # Convert image from BGR2HSV and filter for color range
        hsv_image = cv2.cvtColor(self.current_frame, cv2.COLOR_BGR2HSV)
        color_segmented_image = cv2.inRange(hsv_image, (7, 57, 35), (43, 168, 172))


        # Erode the image
        kernel = np.ones((3, 3), np.uint8)
        eroded_image = cv2.erode(color_segmented_image, kernel)
        kernel = np.ones((5, 5), np.uint8)
        dilated_image = cv2.dilate(eroded_image, kernel)

        if show_images:
            cv2.imshow("Color Segmented Image 2", dilated_image)

        # Find contours in the filtered image
        contours, hierarchy = cv2.findContours(dilated_image, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        # Find the counter with the right area where the y-Coordinate (Sim) is positive
        pusher_contour = None
        if contours:
            for c in contours:
                area = cv2.contourArea(c)
                if 250 < area < 900:
                    (x, y, w, h) = cv2.boundingRect(c)
                    center = self.transform_real2sim(np.array((x + w / 2, y + h / 2)))
                    if -2.1 < center[0] < 2.1:
                        if 0 < center[1] < 4.6:
                            pusher_contour = c
            if np.any(pusher_contour):
                x, y, w, h = cv2.boundingRect(pusher_contour)
                self.pusher_position_pixels = np.array([x + w / 2, y + h / 2])
                self.pusher_position_sim = self.transform_real2sim(self.pusher_position_pixels)
                self.pusher_rectangle = (x, y, w, h)

                cv2.rectangle(self.draw_frame, (x, y), (x + w, y + h), (255, 0, 0), 2)

                if show_images:
                    current_frame_copy = self.current_frame.copy()
                    cv2.rectangle(current_frame_copy, (x, y), (x + w, y + h), (255, 0, 0), 2)

                    cv2.imshow("Pusher Detection Image", current_frame_copy)
            else:
                cv2.putText(self.draw_frame, "no pusher found", (200, 50), cv2.FONT_HERSHEY_PLAIN, 1, (255, 0, 0), 3)

        return
    def transform_real2sim(self, coordinates):
        return np.matmul(np.linalg.inv(self.sim2real_rotation_matrix), (coordinates - self.offset)) / self.scale

    def transform_sim2real(self, coordinates):
        return self.scale * np.matmul(self.sim2real_rotation_matrix, coordinates) + self.offset

    def angle_between(self, vector_one, vector_two):
        # x = arccos((v1 * v2) / (|v1|*|v2|)
        x = np.arccos(np.dot(vector_one, vector_two)/(np.linalg.norm(vector_one)*np.linalg.norm(vector_two)))
        return x*180/np.pi

    def scale_between(self, vector_one, vector_two):
        return np.linalg.norm(vector_one)/np.linalg.norm(vector_two)

    def get_puck_coordinates(self, show_images=False):
        # Convert image from BGR2HSV and filter for color range
        hsv_image = cv2.cvtColor(self.current_frame, cv2.COLOR_BGR2HSV)
        color_segmented_image = cv2.inRange(hsv_image, (42, 52, 35), (83, 255, 255))
        #color_segmented_image = cv2.inRange(hsv_image, (0, 0, 17), (201, 79, 57))
        kernel = np.ones((3, 3), np.uint8)
        eroded_image = cv2.erode(color_segmented_image, kernel)

        #if show_images:
         #   cv2.imshow("Color Segmented Image 3", eroded_image)

        contours, hierarchy = cv2.findContours(eroded_image, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        # Find the biggest contour by area between 300 and 500
        puck_contour = None
        if contours:
            c = max(contours, key=cv2.contourArea)
            if 100 < cv2.contourArea(c) < 500:
                puck_contour = c
        if np.any(puck_contour):
            x, y, w, h = cv2.boundingRect(puck_contour)
            self.puck_position_pixels = np.array([x + w / 2, y + h / 2])
            self.puck_position_sim = self.transform_real2sim(self.puck_position_pixels)
            self.puck_rectangle = (x, y, w, h)

            self.puck_found_time_step = time.time()
            self.puck_not_found = False

            #cv2.rectangle(self.draw_frame, (x, y), (x + w, y + h), (255, 0, 0), 2)
            #current_frame_copy = self.current_frame.copy()
            cv2.rectangle(self.draw_frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
            #self.draw_frame = current_frame_copy



            if show_images:

                cv2.imshow("Puck Detection Image", self.draw_frame)

        else:
            if time.time() - self.puck_found_time_step > 3:
                self.puck_found_time_step = time.time()
                self.puck_position_sim = np.array([0, -2])
                self.puck_not_found = True
            cv2.putText(self.draw_frame, "no puck found", (10, 50), cv2.FONT_HERSHEY_PLAIN, 1, (0, 255, 0), 3)
        return


    def find_calibration_circles(self, show_images=False):
        # Convert image from BGR2HSV and filter for color range
        hsv_image = cv2.cvtColor(self.current_frame, cv2.COLOR_BGR2HSV)
        # ColorPicker says Hue 19, Saturation 68, Value 44
        color_segmented_image = cv2.inRange(hsv_image, (0, 50, 54), (23, 255, 255))

        if show_images:
            cv2.imshow("Color Segmented Image", color_segmented_image)

        # Detect circles in the filtered image:
            # param1: The higher threshold of the two passed to the Canny edge detector.
            # param2: Accumulator threshold for the circle centers at the detection stage.
            #         The smaller it is, the more false circles may be detected.
        circles = cv2.HoughCircles(color_segmented_image, cv2.HOUGH_GRADIENT, 3, 10, minRadius=40, maxRadius=55)
        if circles is None:
            return False
        else:
            circles = np.uint16(np.around(circles))
            if show_images:
                current_frame_copy = self.current_frame.copy()
                for c in circles[0, :]:
                    cv2.circle(current_frame_copy, (c[0], c[1]), c[2], (0, 255, 0), 2)
                    cv2.imshow("Circle Detection Image", current_frame_copy)
            self.calibration_circles = circles
            return True

    def change_vid_mode(self):
        self.livedemo.get()
        if (self.livedemo.get()):
            #self.setVideocap(cv2.VideoCapture(0))
            self.videosource = cv2.VideoCapture(0)
            print("live")
            #self.framerate = self.videosource.get(cv2.cv.CV_CAP_PROPS_FPS)
        else:
            #self.set_video_source(cv2.VideoCapture('pusher_and_puck.mp4'))
            self.videosource = cv2.VideoCapture('pusher_and_puck.mp4')
            print("old video")
            #self.framerate = self.videosource.get(cv2.cv.CV_CAP_PROPS_FPS)

        #print("fps:"+str(self.framerate))
        self.show_frames()


    def show_frames(self):
        if (self.videosource is None):
            print("no vid source jet")
        else:
            try:
                #self.current_frame = cv2.cvtColor(self.videosource.read()[1], cv2.COLOR_BGR2RGB)
                self.current_frame = self.videosource.read()[1]
                self.draw_frame = self.current_frame.copy()
                if self.caliVar.get():
                    self.calibration()
                else:
                    pass
                    if self.calibration_circles is None:

                        print("not calibrated")
                    else:
                        #print("playloop")
                        self.get_puck_coordinates(show_images=False)
                        self.get_pusher_coordinates(show_images=False)
                        #cv2.imshow("drawf",self.draw_frame)
                        if True:
                            self.play_agent()
                            pass


                # hier nur noch anzeige
                try:
                    img = Image.fromarray(cv2.cvtColor(self.draw_frame, cv2.COLOR_BGR2RGB))
                    print("drawimg")
                    #cv2.imshow("drawing", img)
                except:
                    img = Image.fromarray(cv2.cvtColor(self.current_frame, cv2.COLOR_BGR2RGB))
                imgtk = ImageTk.PhotoImage(image=img)
                #cv2.imshow("curr view", self.draw_frame)
                self.LabelVideo.imgtk = imgtk
                self.LabelVideo.configure(image=imgtk)



                self.LabelVideo.after(100, self.show_frames)
                #print("vid")
            except AttributeError:
            #except TypeError:
                print("video over?")
                self.LabelVideo.after(100, self.show_frames)

    def manualmove(self, args):
        print(args)

    def set_video_source(self, vid):
        self.videosource = vid

    def connect_via_serial(self):
        try:
            self.serial_connection.close()
        except:
            print("first serial con")

        print("baudrate:" + self.BaudVar.get())
        print("port:" + self.EntryCOM.get())
        try:
            self.serial_connection = serial.Serial(
                port=self.EntryCOM.get(),
                baudrate=self.BaudVar.get()
            )
            print("INFO: Serial connection established.")
            #return serial_connection
        except SerialException:
            print("WARNING: Serial connection could not be established.")
            #return None


root = Window()


# set window title
root.wm_title("Tkinter window")

#setting params
root.geometry("1000x1000")

root.connect_via_serial()

last_action_send = time.time()
min_action_pause = 0.001




print("ml")
root.mainloop()

