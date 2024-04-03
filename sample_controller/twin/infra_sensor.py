#!/usr/bin/python
# -*- coding: utf-8 -*-

import infra_pdu_info as pdu_info
from math import cos, sin, radians
import numpy as np
from numpy.linalg import lstsq

class InfraSensor:
    def __init__(self, mgr):
        self.max = 2000.0
        self.offset = 100.0
        self.offset_x = 6
        self.base_degree = 179
        self.pdu_manager = mgr
        self.pdu_sensor = self.pdu_manager.get_pdu(pdu_info.SENSOR_NAME, pdu_info.PDU_SCAN_CHANNEL_ID)
    
    def analyze(self, degrees, values):
        x = np.array(degrees)
        z = np.array(values)
        A = np.vstack([x**2, x, np.ones(len(x))]).T
        coefficients, _, _, _ = lstsq(A, z, rcond=None)
        a, b, c = coefficients
        x_vertex = -b / (2 * a)
        z_vertex = a * x_vertex**2 + b * x_vertex + c

        radian_degree = radians(x_vertex - self.base_degree)
        value = z_vertex + self.offset
        x = value * cos(radian_degree) - self.offset_x
        z = value * sin(radian_degree)
        #print(f"(deg, value): ({x_vertex }, {z_vertex})")
        return x, z

    def analyze_min(self):
        value = self.min_value + self.offset
        radian_degree = radians(self.min_deg - self.base_degree)
        x = value * cos(radian_degree)
        z = value * sin(radian_degree)
        return x, z

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
            x, z = self.analyze(degrees, values)
            print(f"analyized (x, z): ({x }, {z})")

            x, z = self.analyze_min()
            print(f"minimum   (x, z): ({x }, {z})")

