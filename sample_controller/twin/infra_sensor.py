#!/usr/bin/python
# -*- coding: utf-8 -*-

import infra_pdu_info as pdu_info
from math import cos, sin, radians
import numpy as np
from numpy.linalg import lstsq

class InfraSensor:
    def __init__(self, mgr):
        self.max = 2000.0
        self.initial_py = 1000.0
        self.initial_px = 0.0
        self.offset = 100.0
        self.offset_x = 6
        self.base_degree = 179
        self.pdu_manager = mgr
        self.pdu_sensor = self.pdu_manager.get_pdu(pdu_info.SENSOR_NAME, pdu_info.PDU_SCAN_CHANNEL_ID)
        self.pdu_pos = self.pdu_manager.get_pdu(pdu_info.AVATAR_NAME, pdu_info.PDU_POS_CHANNEL_ID)
        self.d_pos = self.pdu_pos.get()

    def analyze(self, degrees, values):
        x = np.array(degrees)
        z = np.array(values)
        A = np.vstack([x**2, x, np.ones(len(x))]).T
        coefficients, _, _, _ = lstsq(A, z, rcond=None)
        a, b, c = coefficients
        if a == 0:
            x = 0
            z = 0
            #print(f"a==0:(deg, value): ({x }, {z})")
            return x, z, False
        else:
            x_vertex = -b / (2 * a)
        z_vertex = a * x_vertex**2 + b * x_vertex + c

        radian_degree = radians(x_vertex - self.base_degree)
        value = z_vertex + self.offset
        x = value * cos(radian_degree) - self.offset_x
        z = value * sin(radian_degree)
        #print(f"(deg, value): ({x_vertex }, {z_vertex})")
        return x, z, True

    def analyze_min(self):
        value = self.min_value + self.offset
        radian_degree = radians(self.min_deg - self.base_degree)
        x = value * cos(radian_degree)
        z = value * sin(radian_degree)
        return x, z

    def write_pos(self, zero=False):
        if zero:
            self.d_pos['linear']['x'] = 0
            self.d_pos['linear']['y'] = 0
        else:
            self.d_pos['linear']['x'] = (self.initial_px - self.analyzed_x) / 100.0
            self.d_pos['linear']['y'] = (self.initial_py - self.analyzed_y) / 100.0
        self.d_pos['linear']['z'] = 0
        self.d_pos['angular']['x'] = 0
        self.d_pos['angular']['y'] = 0
        self.d_pos['angular']['z'] = 0
        #print(f"(ax, ay): ({self.analyzed_x }, {self.analyzed_y })")
        #print(f"( x,  y): ({self.d_pos['linear']['x'] }, {self.d_pos['linear']['y'] })")
        self.pdu_pos.write()

    def run(self):
        degrees = []
        values = []

        self.d_sensor = self.pdu_sensor.read()
        self.sensor_values = set()
        i = 0
        self.min_deg = 0
        self.min_value = 10000
        while i < 360:
            if self.d_sensor['ranges'][i] != 2000:
                degrees.append(i)
                values.append(self.d_sensor['ranges'][i])
                if self.min_value > self.d_sensor['ranges'][i]:
                    self.min_value = self.d_sensor['ranges'][i]
                    self.min_deg = i
                self.sensor_values.add((i, self.d_sensor['ranges'][i]))
            i = i + 1
        
        if len(self.sensor_values) > 0:
            self.analyzed_y, self.analyzed_x, result = self.analyze(degrees, values)
            self.write_pos(result == False)
        else:
            self.write_pos(True)

