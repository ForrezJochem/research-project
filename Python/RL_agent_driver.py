import time
import dxcam
import skimage.transform as st
import tensorflow as tf
import win32gui
from pynput.keyboard import Controller, Key
from tensorflow.keras import layers
from pynput import keyboard
from tensorflow import keras

height, width = 128, 227
num_actions = 5
running = False

# the code below is used to get the window of the game
hwnd = win32gui.FindWindow(None, "Grand Theft Auto V")
window = win32gui.GetWindowRect(hwnd)
window = (window[0] + 8, window[1] + 31, window[2] - 8, window[3] - 8)
camera = dxcam.create(output_color="RGB")

# the code below is used to mimic the keyboard
controller = Controller()

def get_screenshot():
    frame = camera.grab(region=(window))
    while frame is None:
        frame = camera.grab(region=(window))
    # resize the image
    frame = st.resize(frame, (height, width))
    return frame

def do_action(action):
    try:
        if (type(action) != int):
            action = int(action)
        if (action == 0):
            controller.press('z')
            time.sleep(0.2)
            controller.release('z')
        elif (action == 1):
            controller.press('s')
            time.sleep(0.2)
            controller.release('s')
        elif (action == 2):
            controller.press('q')
            time.sleep(0.2)
            controller.release('q')
        elif (action == 3):
            controller.press('d')
            time.sleep(0.2)
            controller.release('d')
        elif (action == 4):
            controller.press(Key.space)
            time.sleep(0.2)
            controller.release(Key.space)
    except:
        pass

def create_q_model():
    input = layers.Input(shape=(height, width, 3))

    # convolutions on the frames on the screen
    layer1 = layers.Conv2D(64, 8, activation="relu")(input)
    layer2 = layers.Conv2D(128, 4, activation="relu")(layer1)
    layer3 = layers.Conv2D(256, 4, activation="relu")(layer2)
    layer4 = layers.Flatten()(layer3)
    layer5 = layers.Dense(128, activation="relu")(layer4)
    action = layers.Dense(num_actions, activation="linear")(layer5)
    return keras.Model(inputs=input, outputs=action)

model = create_q_model()
model.load_weights("model.h5")



while True:
    
    action_probs = model.predict(get_screenshot())
    action = tf.argmax(action_probs[0]).numpy()
    print(action)
