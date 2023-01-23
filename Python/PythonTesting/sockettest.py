from http import client
import pickle
import cv2
import numpy as np
import socket
import time

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind(("127.0.0.1", 5000))
s.listen()
clientsocket, address = s.accept()
while True:
    data = clientsocket.recv(1028013)
    print(len(data))
    data = pickle.loads(data)
    #cv2.imshow("image", data)
    #cv2.waitKey(1)
    clientsocket.send(bytes("HTTP/1.1 200 OK", "utf-8"))