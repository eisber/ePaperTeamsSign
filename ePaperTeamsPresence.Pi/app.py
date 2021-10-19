import flask
import sys
import os
import json

picdir = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'pic')
libdir = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'lib')
print(libdir)
if os.path.exists(libdir):
    sys.path.append(libdir)

import logging
from waveshare_epd import epd2in7b
import time
from datetime import datetime

from PIL import Image,ImageDraw,ImageFont
import traceback

# initialize ePaper display
epd = epd2in7b.EPD()
logging.info("init and Clear")
epd.init()

## keep the display content around
# epd.Clear()
# time.sleep(1)

font20 = ImageFont.truetype(os.path.join(picdir, 'Font.ttc'), 20)

app = flask.Flask(__name__)

@app.route("/")
def hello_world():
    return "<p>I am up!</p>"


@app.route("/display", methods=["POST"])
def display():
    file_red = flask.request.files['red']
    file_black = flask.request.files['black']

    if file_red and file_black:
            template = json.loads(flask.request.form.get('template'))

            HBlackimage = Image.open(file_black)
            HRedimage = Image.open(file_red)

            drawred = ImageDraw.Draw(HRedimage)
            drawblack = ImageDraw.Draw(HBlackimage)

            # draw date/time
            now = datetime.now() 
            drawblack.text((template['Time']['X'], template['Time']['Y']), 
                            now.strftime("%H:%M"),
                            font = font20, fill = 0)

            print("updating display")
            epd.display(epd.getbuffer(HBlackimage), epd.getbuffer(HRedimage))            

    return "OK"