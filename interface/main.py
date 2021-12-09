import tkinter as tk
import cv2
import threading
import PIL
import multiprocessing

class Video_process(multiprocessing.Process):
    def __init__(self, id, app):
        super(Video_process, self).__init__()
        self.id = id
        self.app = app

    def run(self):
        self.app.videoframe_update()


class Window(tk.Frame):
    def __init__(self, master=None):
        tk.Frame.__init__(self, master)
        self.master = master
        self.video_cap = None
        self.current_frame = None

        self.imageCanvas = None

        self.Entrybaudrate = tk.Entry()
        self.Labelbaudrate = tk.Label(text="Baudrate")

        self.Entrybaudrate.place(x=100, y=100)
        self.Labelbaudrate.place(x=80, y=100)

    def set_cap(self, cap):
        self.video_cap = cap
        pass

    def videoframe_update(self):
        ret, self.current_frame = self.video_cap.read()
        threading.Timer(0.1, self.videoframe_update()).start()
        print("vid")
        self.canvas_update()


    def canvas_update(self):
        height, width, channels = self.current_frame.shape
        self.imageCanvas = tk.Canvas(width=width, height=height)
        self.imageCanvas.place(x=100, y=120)
        img = PIL.ImageTk.PhotoImage(image=PIL.Image.fromarray(self.current_frame))
        self.imageCanvas.create_image(0, 0, image=img)


# initialize tkinter
root = tk.Tk()
app = Window(root)

# set window title
root.wm_title("Tkinter window")

# show window
root.geometry("1000x500")
app.set_cap(cv2.VideoCapture(0))
video_process = Video_process(0, app)
video_process.start()
app.videoframe_update()
root.mainloop()

