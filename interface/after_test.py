import tkinter as tk
import cv2
import serial
from serial.serialutil import SerialException
import cv2
import numpy as np
from PIL import Image, ImageTk
import time


class Window(tk.Tk):
    def __init__(self, master=None):
        super().__init__()
        self.master = master
        self.framerate = None
        #variables
        self.calibration_circles = None
        self.videosource = None
        self.current_frame = None
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
        self.BaudVar.set(self.BaudChoice[0])

        #gui elements
        self.CheckLive = tk.Checkbutton(self, text="live", variable=self.livedemo, command=self.change_vid_mode)
        self.Checkcalibration = tk.Checkbutton(self, text="calibration", variable=self.caliVar)
        self.Radiomove = tk.Radiobutton(self, text="Auto-Move", variable=self.MoveVar, value=1)
        self.RadioNOmove = tk.Radiobutton(self, text="Manual Move", variable=self.MoveVar, value=2)
        self.Menubau = tk.OptionMenu(self, self.BaudVar, *self.BaudChoice)
        self.EntryCOM = tk.Entry(self, text="port")
        self.LabelVideo = tk.Label(self)

        self.ButtonMoveleft = tk.Button(self, text="left", command=lambda: self.manualmove(1))
        self.ButtonMoveright = tk.Button(self, text="right", command=lambda: self.manualmove(2))
        self.ButtonMoveup = tk.Button(self, text="up", command=lambda: self.manualmove(3))
        self.ButtonMovedown = tk.Button(self, text="down", command=lambda: self.manualmove(4))
        self.ButtonMovestop = tk.Button(self, text="stop", command=lambda: self.manualmove(0))
        self.ButtonSerialStart = tk.Button(self, text="serial start", command=self.connect_via_serial)


        self.LabelVideo.place(x=200, y=10)
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
        cali = self.find_calibration_circles(True)
        if cali:
            print("cali done")
            self.caliVar.set(False)
        else:
            print("cali failed")


    def find_calibration_circles(self, show_images=False):
        # Convert image from BGR2HSV and filter for color range
        hsv_image = cv2.cvtColor(self.current_frame, cv2.COLOR_RGB2HSV)
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
                self.current_frame = cv2.cvtColor(self.videosource.read()[1], cv2.COLOR_BGR2RGB)
                if self.caliVar.get():
                    self.calibration()
                img = Image.fromarray(self.current_frame)
                imgtk = ImageTk.PhotoImage(image=img)
                self.LabelVideo.imgtk = imgtk
                self.LabelVideo.configure(image=imgtk)
                self.LabelVideo.after(100, self.show_frames)
                #print("vid")
            except:
                print("video over?")

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

