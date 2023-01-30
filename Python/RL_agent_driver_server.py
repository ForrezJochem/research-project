import json
import pickle
import socket
import numpy as np
import tkinter as tk
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers


port_python_client = 5001
height, width = 128, 227
num_actions = 5


def create_q_model():
    input = layers.Input(shape=(height, width, 3))

    # convolutions on the frames on the screen
    layer1 = layers.Conv2D(64, 8, strides=4, activation="relu")(input)
    layer2 = layers.Conv2D(128, 4, strides=2, activation="relu")(layer1)
    layer3 = layers.Conv2D(256, 4, strides=1, activation="relu")(layer2)
    layer4 = layers.Flatten()(layer3)
    layer5 = layers.Dense(128, activation="relu")(layer4)
    action = layers.Dense(num_actions, activation="linear")(layer5)
    return keras.Model(inputs=input, outputs=action)

model = create_q_model()
model.load_weights("model.h5")

print("attempting to connect to python client")
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind(("0.0.0.0", port_python_client))
s.listen()
clientsocketpython, address = s.accept()
print("connected to python client")


def send_python_data(data):
    try:
        data = json.dumps(int(data))
        clientsocketpython.send(bytes(data, "utf-8"))
        data = clientsocketpython.recv(1024)
        data = data.decode("utf-8")
        if (data == "true"):
            return
    except:
        print("error in json dump")


def get_gta_image():
    while True:
        try:
            clientsocketpython.send(bytes("true", "utf-8"))
            data = clientsocketpython.recv(697540)
            data = pickle.loads(data)
            break
        except:
            pass
    return data

while True:
    state_tensor = tf.convert_to_tensor(get_gta_image())
    state_tensor = tf.expand_dims(state_tensor, 0)
    action_probs = np.argmax(model(state_tensor, training=False)[0])
    send_python_data(tf.argmax(action_probs[0]).numpy())
