import socket
from http import client
import pickle
import win32gui
from matplotlib import pyplot as plt
from skimage.transform import resize
import numpy as np
import dxcam

# create a socket object
serversocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
serversocket.connect(("127.0.0.1", 5000))


hwnd = win32gui.FindWindow(None, "Grand Theft Auto V")
window = win32gui.GetWindowRect(hwnd)
window = (window[0] + 8, window[1] + 31, window[2] - 8, window[3] - 8)
camera = dxcam.create(output_color="GRAY")
while True:
    frame = camera.grab(region=(window))
    if frame is None:
        continue
    len(pickle.dumps(frame))
    serversocket.send(pickle.dumps(frame))
    data = serversocket.recv(15)
    print(data)
    
    
    
