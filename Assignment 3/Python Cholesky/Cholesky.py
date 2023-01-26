import cv2
import numpy as np

H1 = np.array([[0.11875, 0.003125, 1263],
               [-0.009375, 0.109375, 579],
               [0, 0, 1]])
H2 = np.array([[-0.2069876688197294, -0.01163571638285381, 1582.698179682912],
               [-0.07911039342337028, 0.09665847034644749, 567.7621843805049],
               [-0.0001651497357604225, -5.504991192014111e-06, 1]])
H3 = np.array([[0.1102699880668267, -0.04530877088305475, 1262.665871121718],
               [-0.01192571599045313, -0.002080847255369791, 781.2243436754175],
               [-7.458233890214379e-06, -6.712410501193314e-05, 1]])


def create_vmat(H, i, j):
    V = np.array([H[0][i-1]*H[0][j-1],
                  H[0][i-1]*H[1][j-1] + H[1][i-1]*H[0][j-1],
                  H[0][i-1]*H[2][j-1] + H[2][i-1]*H[0][j-1],
                  H[1][i-1]*H[1][j-1],
                  H[1][i-1]*H[2][j-1] + H[2][i-1]*H[1][j-1],
                  H[2][i-1]*H[2][j-1]])
    return V


V1_11 = create_vmat(H1, 1, 1)
V1_12 = create_vmat(H1, 1, 2)
V1_22 = create_vmat(H1, 2, 2)
V2_11 = create_vmat(H2, 1, 1)
V2_12 = create_vmat(H2, 1, 2)
V2_22 = create_vmat(H2, 2, 2)
V3_11 = create_vmat(H3, 1, 1)
V3_12 = create_vmat(H3, 1, 2)
V3_22 = create_vmat(H3, 2, 2)

V = np.array([V1_12, V1_11 - V1_22, V2_12, V2_11 - V2_22, V3_12, V3_11 - V3_22])

u, s, vh = np.linalg.svd(V)

B = np.array([[s[0], s[1], s[2]],
              [s[1], s[3], s[4]],
              [s[2], s[4], s[5]]])

# B = np.array([[0.05807825408518712, 0.02638392378415696, 0.008378798823006205],
#               [0.02638392378415696, 2.505546293677046e-05, 7.842437442305525e-06],
#               [0.008378798823006205, 7.842437442305525e-06, 2.740431220951377e-09]])

A = np.linalg.cholesky(B)