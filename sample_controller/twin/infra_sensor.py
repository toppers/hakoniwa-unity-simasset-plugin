#!/usr/bin/python
# -*- coding: utf-8 -*-

import infra_pdu_info as pdu_info
from math import cos, sin, radians
import numpy as np
from numpy.linalg import lstsq

class InfraSensor:
    def __init__(self, mgr):
        #LiDAR Params
        self.contact_max = 1000.0
        self.sensor_pos_y = 1300.0
        self.sensor_pos_x = 0.0
        self.offset_distance = 100.0
        self.offset_x = 0
        self.offset_y = 0
        self.base_degree = 179

        self.pdu_manager = mgr
        self.pdu_sensor = self.pdu_manager.get_pdu(pdu_info.SENSOR_NAME, pdu_info.PDU_SCAN_CHANNEL_ID)
        self.pdu_pos = self.pdu_manager.get_pdu(pdu_info.AVATAR_NAME, pdu_info.PDU_POS_CHANNEL_ID)
        self.d_pos = self.pdu_pos.get()

    def analyze(self, degrees, values):
        deg = np.array(degrees)
        distance = np.array(values)
        A = np.vstack([deg**2, deg, np.ones(len(deg))]).T
        coefficients, _, _, _ = lstsq(A, distance, rcond=None)
        a, b, c = coefficients
        if a == 0:
            return 0, 0, False
        else:
            deg_vertex = -b / (2 * a)
        distance_vertex = a * deg_vertex**2 + b * deg_vertex + c

        radian_degree = radians(self.base_degree - deg_vertex)
        value = distance_vertex + self.offset_distance
        y = value * cos(radian_degree) + self.offset_y
        x = value * sin(radian_degree) + self.offset_x
        #print(f"(y, x): ({y}, {x})")
        return y, x, True

    def analyze_circle(self, degrees, values):
        pos_x = []
        pos_y = []
        y = 0
        x = 0
        R = 100
        for degree, value in zip(degrees, values):
            radian_degree = radians(self.base_degree - degree)
            pos_y.append(value * cos(radian_degree))
            pos_x.append(value * sin(radian_degree))
        x_data = np.array(pos_x)
        y_data = np.array(pos_y)
        A = np.vstack([x_data, y_data, np.ones(len(x_data))]).T
        B = -x_data**2 - y_data**2 -R**2
        params, residuals, rank, s = np.linalg.lstsq(A, B, rcond=None)
        D, E, F = params
        h = -D / 2
        k = -E / 2
        #r = np.sqrt(h**2 + k**2 - F)

        print(f"Center: ({h}, {k}), Radius: {R}")
        return k, h, True
    
    def write_pos(self, zero=False):
        if zero:
            self.d_pos['linear']['x'] = 0
            self.d_pos['linear']['y'] = 0
        else:
            #self.d_pos['linear']['x'] = (self.sensor_pos_x - self.analyzed_x) / 100.0
            #self.d_pos['linear']['y'] = (self.sensor_pos_y - self.analyzed_y) / 100.0
            self.d_pos['linear']['x'] = (self.sensor_pos_x - self.analyzed_x) / 100.0
            self.d_pos['linear']['y'] = (self.sensor_pos_y - self.analyzed_y) / 100.0
        self.d_pos['linear']['z'] = 0.63
        self.d_pos['angular']['x'] = 0
        self.d_pos['angular']['y'] = 0
        self.d_pos['angular']['z'] = 0
        print(f"(ax, ay): ({self.analyzed_x }, {self.analyzed_y })")
        print(f"( x,  y): ({self.d_pos['linear']['x'] }, {self.d_pos['linear']['y'] })")
        self.pdu_pos.write()

    def run(self):
        degrees = []
        values = []

        self.d_sensor = self.pdu_sensor.read()
        i = 0
        self.min_deg = 0
        self.min_value = 10000
        while i < 360:
            if self.d_sensor['ranges'][i] != self.contact_max:
                #print(f"({i} , {self.d_sensor['ranges'][i]})")
                degrees.append(i)
                values.append(self.d_sensor['ranges'][i])
            i = i + 1
        
        if len(degrees) > 0:
            #self.analyzed_y, self.analyzed_x, result = self.analyze(degrees, values)
            self.analyzed_y, self.analyzed_x, result = self.analyze_circle(degrees, values)
            self.write_pos(result == False)
        else:
            self.write_pos(True)

