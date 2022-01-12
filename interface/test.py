import tkinter as tk
import cv2
import numpy
import serial
from serial.serialutil import SerialException
import cv2
import numpy as np
from PIL import Image, ImageTk
import time
import onnx
import onnxruntime
import torch

#from onnx_tf.backend import prepare

#onnx_model = onnx.load("opponent.onnx")  # load onnx model
#tf_rep = prepare(onnx_model)
#print(tf_rep.inputs) # Input nodes to the model
#print(tf_rep.outputs) # Output nodes from the model

#['version_number', 'memory_size', 'discrete_actions', 'discrete_action_output_shape']
agent = onnxruntime.InferenceSession("opponent.onnx")
print(agent.get_inputs()[0])
print(agent.get_inputs()[1])
print(agent.get_outputs()[0])

#print(type(torch.tensor([[1., -1.], [1., -1.]])))
arr_obs = np.array([[1,2,3,4,5,6,7,8]],dtype=np.float32)
arr_am = np.array([[0,0,0,0,0,0,0,0,0]],dtype=np.float32)
ortvalue = onnxruntime.OrtValue.ortvalue_from_numpy(arr_obs, 'cpu', 0)
ortvalue_am = onnxruntime.OrtValue.ortvalue_from_numpy(arr_am, 'cpu', 0)


print(ortvalue.device_name())
print(ortvalue.data_type())
print(ortvalue_am.shape())
print(ortvalue.is_tensor())
outputs = agent.run(['discrete_actions'], {'obs_0': ortvalue, 'action_masks': ortvalue_am})

print(outputs[0][0][0])
#x = numpy.array([3,],dtype=numpy.float32)
#print(x)

