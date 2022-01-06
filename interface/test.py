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
import torch

agent = onnxruntime.InferenceSession("opponent.onnx")
print(agent.get_inputs()[0])
print(agent.get_inputs()[1])
print(agent.get_outputs()[0])

#print(type(torch.tensor([[1., -1.], [1., -1.]])))
arr_obs = np.array([[1,2,3,4,5,6,7,8]],dtype=np.float32)
arr_am = np.array([[1,2,3,4,5,6,7,8,9]],dtype=np.float32)
ortvalue = onnxruntime.OrtValue.ortvalue_from_numpy(arr_obs, 'cpu', 0)
ortvalue_am = onnxruntime.OrtValue.ortvalue_from_numpy(arr_am, 'cpu', 0)


print(ortvalue.device_name())
print(ortvalue.data_type())
print(ortvalue_am.shape())
print(ortvalue.is_tensor())
outputs = agent.run(['version_number'], {'obs_0': ortvalue, 'action_masks': ortvalue_am})
print(outputs[0][0])