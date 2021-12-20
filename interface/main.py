import tkinter as tk
import cv2
import threading
#import PIL
from PIL import Image as Pil_image, ImageTk as Pil_imageTk
import threading


class Window(tk.Tk):
    def __init__(self, master=None):
        super().__init__()
        self.master = master
        self.video_cap = None
        self.current_frame = None
        self.toggle = True
        self.tkimg = None

        self.MoveVar = tk.IntVar()
        self.MoveVar.set(2)
        self.BaudChoice = [
            "3",
            "4",
            "8"
        ]
        self.BaudVar = tk.StringVar()
        self.BaudVar.set(self.BaudChoice[0])

        self.imageCanvas = None
        self.testimg = Pil_imageTk.PhotoImage(Pil_image.open("test_pic.PNG"))
        self.camimg = Pil_imageTk.PhotoImage(Pil_image.open("temp.PNG"))

        self.Frameimg = tk.LabelFrame(self, text="Camera")
        self.Labelimg = tk.Label(self.Frameimg, width=650, height=500, bg="red")

        self.Radiomove = tk.Radiobutton(self, text="Move", variable=self.MoveVar, value=1)
        self.RadioNOmove = tk.Radiobutton(self, text="Dont Move", variable=self.MoveVar, value=2)
        self.Menubau = tk.OptionMenu(self, self.BaudVar, *self.BaudChoice)


        self.Labelbaudrate = tk.Label(self, text="Baudrate")
        self.Canvasimg = None

        self.Frameimg.place(x=200, y=200)
        self.Labelbaudrate.place(x=80, y=200)
        self.Labelimg.pack()
        self.Radiomove.place(x=80, y=80)
        self.RadioNOmove.place(x=80, y=60)
        self.Menubau.place(x=140, y=100)


        self.Labelimg.after(1000,self.update)

    def set_cap(self, cap):
        self.video_cap = cap
        pass

    def cv2_to_pil(self, img):
        return Pil_image.fromarray(cv2.cvtColor(img, cv2.COLOR_BGR2RGB))

        #return Pil_image.fromarray(img)

    def videoframe_update(self):
        if (self.video_cap != None):
            ret, self.current_frame = self.video_cap.read()
            #cv2.imwrite('..path\\temp.PNG', self.current_frame)
            #cv2.imshow("vid", self.current_frame)
            print("vid")
            pil_img = self.cv2_to_pil(self.current_frame) #to pil img
            #img = Pil_imageTk.PhotoImage(pil_img)         #to tk img
            img = Pil_imageTk.PhotoImage(Pil_image.open("temp.PNG"))
            self.tkimg = img
            #pil_img.show()

            return img
        else:
            self.video_cap = cv2.VideoCapture(0)
            return self.testimg


    def update(self):
        try:
            img = self.videoframe_update()
            if self.toggle:
                self.toggle = False
                self.Labelimg['image'] = self.camimg
            else:
                self.toggle = True
                self.Labelimg['image'] = self.tkimg

            print("pic updated")

        except:
            print("No image")


        self.Labelimg.after(1000, self.update)



# initialize tkinter
root = Window()


# set window title
root.wm_title("Tkinter window")

# show window
root.geometry("1000x1000")
root.set_cap(cv2.VideoCapture(0))

root.mainloop()

