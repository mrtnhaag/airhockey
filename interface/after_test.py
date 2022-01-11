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

        self.pusher_rectangle = None


        #variables nn
        self.agent = onnxruntime.InferenceSession("opponent.onnx")
        self.action_last = 0
        #variables transform
        self.scale = None
        self.angle_deg = None
        self.angle_rad = None
        self.offset = None
        #rest variables
        self.rate_gui = 20 #ms pro durchlauf
        self.fieldwidth = 1.8
        self.fieldlength = 4
        self.puck_rectangle = None
        self.puck_found_time_step = 0
        self.pusher_found_time_step = 0
        self.videosource = None
        self.current_frame = None
        self.draw_frame = None
        self.serial_connection = None
        self.pusher_position_sim = [0, 0]
        self.pusher_position_sim_last = [0, 0]
        self.puck_position_sim = [0, 0]
        self.puck_position_sim_last = [0, 0]
        self.puck_vel_sim = [0, 0]
        self.pusher_vel_sim = [0, 0]
        self.puck_not_found = True
        self.pusher_not_found = True
        self.VideomodeVar = tk.IntVar()
        self.VideomodeVar.set(1)

        #serial vars
        self.serial_connected = False
        self.caliVar = tk.BooleanVar()
        self.MoveVar = tk.IntVar()
        self.MoveVar.set(2)
        self.BaudChoice = [
            "4800",
            "9600",
            "57600",
            "74880",
            "115200",
            "230400"
        ]
        self.BaudVar = tk.StringVar()
        self.StatusVar = tk.StringVar()
        self.StatusVar.set("calib not started")
        self.StatusVarObs = tk.StringVar()
        self.StatusVarObs.set("Observations")
        self.StatusPlayVar = tk.StringVar()
        self.StatusPlayVar.set("init playstate")
        self.StatusSerialVar = tk.StringVar()
        self.StatusSerialVar.set("serial not set")
        self.BaudVar.set(self.BaudChoice[0])

        #gui elements
        self.Checkcalibration = tk.Checkbutton(self, text="calibration", variable=self.caliVar)
        self.Radiomove = tk.Radiobutton(self, text="Auto-Move", variable=self.MoveVar, value=1)
        self.RadioNOmove = tk.Radiobutton(self, text="Manual Move", variable=self.MoveVar, value=2)
        self.Radiolive = tk.Radiobutton(self, text="live ", variable=self.VideomodeVar, value=1, command=self.change_vid_mode)
        self.Radiooldvideo = tk.Radiobutton(self, text="video playback", variable=self.VideomodeVar, value=2, command=self.change_vid_mode)
        self.Menubau = tk.OptionMenu(self, self.BaudVar, *self.BaudChoice)
        self.EntryCOM = tk.Entry(self, text="port")
        self.LabelVideo = tk.Label(self)

        self.LabelDirNN = tk.Label(self)
        imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_nix.png")
        self.LabelDirNN.imgtk = imgtk
        self.LabelDirNN.configure(image=imgtk)

        self.LabelStatus = tk.Label(self, textvariable=self.StatusVar)
        self.LabelStatusObs = tk.Label(self, textvariable=self.StatusVarObs)
        self.LabelStatusPlay = tk.Label(self, textvariable=self.StatusPlayVar)
        self.LabelStatusSerial = tk.Label(self, textvariable=self.StatusSerialVar)

        self.ButtonMoveleft = tk.Button(self, text="left", command=lambda: self.manualmove(4))
        self.ButtonMoveright = tk.Button(self, text="right", command=lambda: self.manualmove(5))
        self.ButtonMoveup = tk.Button(self, text="up", command=lambda: self.manualmove(2))
        self.ButtonMovedown = tk.Button(self, text="down", command=lambda: self.manualmove(7))
        self.ButtonMovestop = tk.Button(self, text="stop", command=lambda: self.manualmove(0))
        self.ButtonSerialStart = tk.Button(self, text="serial start", command=self.connect_via_serial)


        self.LabelVideo.place(x=200, y=10)
        self.LabelDirNN.place(x=200, y=550)

        self.LabelStatus.place(x=10, y=400)
        self.LabelStatusObs.place(x=400, y=570)
        self.LabelStatusSerial.place(x=10, y=420)
        self.LabelStatusPlay.place(x=10, y=440)
        self.Radiomove.place(x=20, y=20)
        self.RadioNOmove.place(x=20, y=40)
        self.Radiooldvideo.place(x=70, y=300)
        self.Radiolive.place(x=20, y=300)
        self.ButtonMoveleft.place(x=10, y=100)
        self.ButtonMoveright.place(x=90, y=100)
        self.ButtonMoveup.place(x=50, y=70)
        self.ButtonMovedown.place(x=50, y=130)
        self.ButtonMovestop.place(x=50, y=100)
        self.EntryCOM.place(x=10, y=200)
        self.Menubau.place(x=10, y=220)
        self.ButtonSerialStart.place(x=10, y=250)
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
        # observations as following:
        #0,1,2. agent x,y,z
        #3,4,5 puck x,y,z
        #6,7 puck vx, vy
        arr_obs = [self.pusher_position_sim[0], self.pusher_position_sim[1], 0, self.puck_position_sim[0], self.puck_position_sim[1], 0, self.puck_vel_sim[0], self.puck_vel_sim[1]]
        self.StatusVarObs.set(str(np.round(arr_obs,2)))
        arr_obs_np = np.array([arr_obs], dtype=np.float32)
        arr_am_np = np.array([[1, 2, 3, 4, 5, 6, 7, 8, 9]], dtype=np.float32) # actionmask, was das?
        ortvalue_obs = onnxruntime.OrtValue.ortvalue_from_numpy(arr_obs_np, 'cpu', 0)
        ortvalue_am = onnxruntime.OrtValue.ortvalue_from_numpy(arr_am_np, 'cpu', 0)
        #print(ortvalue_am.shape())

        outputs = self.agent.run(['version_number'], {'obs_0': ortvalue_obs, 'action_masks': ortvalue_am})

        self.action_last = outputs[0][0]
        self.act_serial(outputs[0][0])
        #print(outputs[0][0])

        pass

    def act_serial(self, move):
        if self.pusher_position_sim[0] > self.fieldwidth: #zu weit rechts
            if move == 3:
                move = 2
            if move == 5:
                move = 0
            if move == 8:
                move = 7
        if self.pusher_position_sim[0] < -self.fieldwidth:# zu links
            if move == 1:
                move = 2
            if move == 4:
                move = 0
            if move == 6:
                move = 7
            pass
        if self.pusher_position_sim[1] > -0.3:# zu weit vorne
            if move == 1:
                move = 4
            if move == 2:
                move = 0
            if move == 3:
                move = 5
            pass
        if self.pusher_position_sim[1] < -self.fieldlength: #zu weit hinten
            if move == 6:
                move = 4
            if move == 7:
                move = 0
            if move == 8:
                move = 5
            pass
        if move == 0:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_nix.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 1:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_up_left.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 2:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_up.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 3:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_up_right.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 4:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_left.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 5:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_right.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 6:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_down_left.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 7:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_down.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        elif move == 8:
            imgtk = ImageTk.PhotoImage(file="arrow_pics/arrow_down_right.png")
            self.LabelDirNN.imgtk = imgtk
            self.LabelDirNN.configure(image=imgtk)
        self.write_serial(move, False)
        #print(move)

    def movehome(self):
        homegoal = [0, -2]
        toleranz = 0.2
        dir = np.array(homegoal)-self.pusher_position_sim
        move = 0
        if np.sqrt(dir.dot(dir))> toleranz:
            if dir[0] > 0:
                move = 4
            elif dir[0] < 0:
                move = 5
            if dir[1] > 0:
                move = 4
            elif dir[1] < 0:
                move = 5
        self.act_serial(move)

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
        #color_segmented_image = cv2.inRange(hsv_image, (7, 57, 35), (43, 168, 172))
        color_segmented_image = cv2.inRange(hsv_image, (9, 57, 35), (50, 106, 172))



        # Erode the image
        kernel3 = np.ones((3, 3), np.uint8)
        kernel5 = np.ones((5, 5), np.uint8)
        kernel7 = np.ones((7, 7), np.uint8)
        color_segmented_image = cv2.dilate(color_segmented_image, kernel3)
        eroded_image = cv2.erode(color_segmented_image, kernel7)
        kernel = np.ones((5, 5), np.uint8)
        dilated_image = cv2.dilate(eroded_image, kernel5)

        if show_images:
            cv2.imshow("Color Segmented Image 2", dilated_image)

        # Find contours in the filtered image
        contours, hierarchy = cv2.findContours(dilated_image, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)

        # Find the counter with the right area where the y-Coordinate (Sim) is positive
        pusher_contour = None
        if contours:
            for c in contours:
                area = cv2.contourArea(c)
                if 80 < area < 700:
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

                delta_time = time.time() - self.pusher_found_time_step
                delta_pos = self.pusher_position_sim - self.pusher_position_sim_last
                self.pusher_vel_sim = delta_pos / delta_time

                self.pusher_found_time_step = time.time()
                self.pusher_position_sim_last = self.pusher_position_sim
                self.pusher_not_found = False

                if show_images:
                    current_frame_copy = self.current_frame.copy()
                    cv2.rectangle(current_frame_copy, (x, y), (x + w, y + h), (255, 0, 0), 2)

                    cv2.imshow("Pusher Detection Image", current_frame_copy)
            else:
                self.pusher_not_found = True
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

        if show_images:
            cv2.imshow("Color Segmented Image 3", eroded_image)

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


            self.puck_not_found = False

            cv2.rectangle(self.draw_frame, (x, y), (x + w, y + h), (0, 255, 0), 2)

            delta_time = time.time()-self.puck_found_time_step
            delta_pos = self.puck_position_sim-self.puck_position_sim_last
            self.puck_vel_sim = delta_pos/delta_time

            self.puck_found_time_step = time.time()
            self.puck_position_sim_last = self.puck_position_sim


            if show_images:

                cv2.imshow("Puck Detection Image", self.draw_frame)

        else:
            if time.time() - self.puck_found_time_step > 3:
                pass
                #self.puck_found_time_step = time.time()
                #self.puck_position_sim = np.array([0, -2])
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

        if (self.VideomodeVar.get()==1):
            if self.videosource is None:
                pass
            else:
                self.videosource.release()
            #self.setVideocap(cv2.VideoCapture(0))
            self.videosource = cv2.VideoCapture(0)
            print("live")
            #self.framerate = self.videosource.get(cv2.cv.CV_CAP_PROPS_FPS)
        else:
            #self.set_video_source(cv2.VideoCapture('pusher_and_puck.mp4'))
            if self.videosource is None:
                pass
            else:
                self.videosource.release()
            self.videosource = cv2.VideoCapture('pusher_and_puck.mp4')
            print("old video")
            #self.framerate = self.videosource.get(cv2.cv.CV_CAP_PROPS_FPS)

        #print("fps:"+str(self.framerate))
        print("next frames")
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
                        pass

                        #print("not calibrated")
                    else:
                        #print("playloop")
                        self.get_puck_coordinates(show_images=False)
                        self.get_pusher_coordinates(show_images=False)
                        #cv2.imshow("drawf",self.draw_frame)
                        if self.pusher_not_found or self.puck_not_found: #nicht beide gefunden
                            #print("pusher or puck not found")
                            if time.time()-self.puck_found_time_step < 0.1 and time.time()-self.pusher_found_time_step < 0.1:#aber noch nicht lange her
                                self.StatusPlayVar.set("last action replay")
                                self.act_serial(self.action_last)
                                pass

                            else:
                                if time.time()-self.pusher_found_time_step > 0.5: #pusher zu lange nicht gefunden, crash
                                    self.StatusPlayVar.set("pusher missing, danger")
                                    self.act_serial(0)
                                else:                   #sieht nach pause aus, home
                                    self.StatusPlayVar.set("homing..")
                                    self.movehome()
                        else:           #alles gut, spielen
                            self.StatusPlayVar.set("playing..")
                            self.play_agent()


                            pass


                # hier nur noch anzeige
                try:
                    img = Image.fromarray(cv2.cvtColor(self.draw_frame, cv2.COLOR_BGR2RGB))
                except:
                    img = Image.fromarray(cv2.cvtColor(self.current_frame, cv2.COLOR_BGR2RGB))
                imgtk = ImageTk.PhotoImage(image=img)
                #cv2.imshow("curr view", self.draw_frame)
                self.LabelVideo.imgtk = imgtk
                self.LabelVideo.configure(image=imgtk)



                self.LabelVideo.after(self.rate_gui, self.show_frames)
                #print("vid")
            except AttributeError:
            #except TypeError:
                print("video over?")
                self.LabelVideo.after(self.rate_gui, self.show_frames)

    def manualmove(self, args):
        if self.MoveVar.get() == 2:
            self.write_serial(args, True)
        if self.MoveVar.get() == 1:
            print("automove")

    def set_video_source(self, vid):
        self.videosource = vid

    def connect_via_serial(self):
        try:
            self.serial_connection.close()
            self.serial_connected = False
            self.StatusSerialVar.set("serial not set!!!")

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
            self.serial_connected = True
            self.StatusSerialVar.set("serial connection set")

        except SerialException:
            print("WARNING: Serial connection could not be established.")
            self.serial_connected = False
            self.StatusSerialVar.set("serial not set!!!")

            #return None

    def write_serial(self, move, handmove):
        #if self.serial_connected:
        handmode = True
        if self.MoveVar.get() == 1:
            handmode = False
        if handmove == handmode:
            #self.serial_connection.write(bytes(str(move), 'utf-8'))
            #self.serial_connection.flush()
            pass

        else:
            pass
            self.StatusSerialVar.set("serial not set!!!")






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

