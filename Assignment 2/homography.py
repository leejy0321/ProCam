import cv2
import numpy as np

####### Feature Points #######
img1 = {'le' : (1440,446), 're' : (1554,444), 'n' : (1531,555), 'll' : (1440,605), 'rl' : (1540,595)}
img2 = {'le' : (849,478), 're' : (954,478), 'n' : (933,581), 'll' : (856,633), 'rl' : (942,622)}
img3 = {'le' : (594,530), 're' : (762,527), 'n' : (652,673), 'll' : (631,730), 'rl' : (759,720)}

####### Question 2 #######
A1 = np.array([[-img1['le'][0], -img1['le'][1], -1, 0, 0, 0, img1['le'][0]*img2['le'][0], img1['le'][1]*img2['le'][0], img2['le'][0]],
               [0, 0, 0, -img1['le'][0], -img1['le'][1], -1, img1['le'][0]*img2['le'][1], img1['le'][1]*img2['le'][1], img2['le'][1]]])
A2 = np.array([[-img1['re'][0], -img1['re'][1], -1, 0, 0, 0, img1['re'][0]*img2['re'][0], img1['re'][1]*img2['re'][0], img2['re'][0]],
               [0, 0, 0, -img1['re'][0], -img1['re'][1], -1, img1['re'][0]*img2['re'][1], img1['re'][1]*img2['re'][1], img2['re'][1]]])
A3 = np.array([[-img1['n'][0], -img1['n'][1], -1, 0, 0, 0, img1['n'][0]*img2['n'][0], img1['n'][1]*img2['n'][0], img2['n'][0]],
               [0, 0, 0, -img1['n'][0], -img1['n'][1], -1, img1['n'][0]*img2['n'][1], img1['n'][1]*img2['n'][1], img2['n'][1]]])
A4 = np.array([[-img1['ll'][0], -img1['ll'][1], -1, 0, 0, 0, img1['ll'][0]*img2['ll'][0], img1['ll'][1]*img2['ll'][0], img2['ll'][0]],
               [0, 0, 0, -img1['ll'][0], -img1['ll'][1], -1, img1['ll'][0]*img2['ll'][1], img1['ll'][1]*img2['ll'][1], img2['ll'][1]]])
A5 = np.array([[-img1['rl'][0], -img1['rl'][1], -1, 0, 0, 0, img1['rl'][0]*img2['rl'][0], img1['rl'][1]*img2['rl'][0], img2['rl'][0]],
               [0, 0, 0, -img1['rl'][0], -img1['rl'][1], -1, img1['rl'][0]*img2['rl'][1], img1['rl'][1]*img2['rl'][1], img2['rl'][1]]])
A = np.concatenate([A1, A2, A3, A4, A5], axis=0)

U, s, V = np.linalg.svd(A, full_matrices=True)
h = V[8,:]
H = h.reshape(3,3)

image1 = cv2.imread("img1.jpg")
image2 = cv2.imread("img2.jpg")
syn_image2 = cv2.warpPerspective(image1, H, (1920,1080))
cv2.imshow("syn_image", syn_image2)
cv2.waitKey()
cv2.destroyAllWindows()

####### Question 3 #######
A1 = np.array([[-img1['le'][0], -img1['le'][1], -1, 0, 0, 0, img1['le'][0]*img3['le'][0], img1['le'][1]*img3['le'][0], img3['le'][0]],
               [0, 0, 0, -img1['le'][0], -img1['le'][1], -1, img1['le'][0]*img3['le'][1], img1['le'][1]*img3['le'][1], img3['le'][1]]])
A2 = np.array([[-img1['re'][0], -img1['re'][1], -1, 0, 0, 0, img1['re'][0]*img3['re'][0], img1['re'][1]*img3['re'][0], img3['re'][0]],
               [0, 0, 0, -img1['re'][0], -img1['re'][1], -1, img1['re'][0]*img3['re'][1], img1['re'][1]*img3['re'][1], img3['re'][1]]])
A3 = np.array([[-img1['n'][0], -img1['n'][1], -1, 0, 0, 0, img1['n'][0]*img3['n'][0], img1['n'][1]*img3['n'][0], img3['n'][0]],
               [0, 0, 0, -img1['n'][0], -img1['n'][1], -1, img1['n'][0]*img3['n'][1], img1['n'][1]*img3['n'][1], img3['n'][1]]])
A4 = np.array([[-img1['ll'][0], -img1['ll'][1], -1, 0, 0, 0, img1['ll'][0]*img3['ll'][0], img1['ll'][1]*img3['ll'][0], img3['ll'][0]],
               [0, 0, 0, -img1['ll'][0], -img1['ll'][1], -1, img1['ll'][0]*img3['ll'][1], img1['ll'][1]*img3['ll'][1], img3['ll'][1]]])
A5 = np.array([[-img1['rl'][0], -img1['rl'][1], -1, 0, 0, 0, img1['rl'][0]*img3['rl'][0], img1['rl'][1]*img3['rl'][0], img3['rl'][0]],
               [0, 0, 0, -img1['rl'][0], -img1['rl'][1], -1, img1['rl'][0]*img3['rl'][1], img1['rl'][1]*img3['rl'][1], img3['rl'][1]]])
A = np.concatenate([A1, A2, A3, A4, A5], axis=0)

U, s, V = np.linalg.svd(A, full_matrices=True)
h = V[8,:]
H = h.reshape(3,3)

image1 = cv2.imread("img1.jpg")
image3 = cv2.imread("img3.jpg")
syn_image3 = cv2.warpPerspective(image1, H, (1920,1080))
cv2.imshow("syn_image", syn_image3)
cv2.waitKey()
cv2.destroyAllWindows()

####### EXTRA #######
img4 = {'le' : (1439,446), 're' : (1554,444), 'n' : (1523,551), 'll' : (1441,605), 'e' : (1217,487)}
img5 = {'le' : (849,478), 're' : (953,478), 'n' : (925,579), 'll' : (855,633), 'e' : (635,516)}

A1 = np.array([[-img4['le'][0], -img4['le'][1], -1, 0, 0, 0, img4['le'][0]*img5['le'][0], img4['le'][1]*img5['le'][0], img5['le'][0]],
               [0, 0, 0, -img4['le'][0], -img4['le'][1], -1, img4['le'][0]*img5['le'][1], img4['le'][1]*img5['le'][1], img5['le'][1]]])
A2 = np.array([[-img4['re'][0], -img4['re'][1], -1, 0, 0, 0, img4['re'][0]*img5['re'][0], img4['re'][1]*img5['re'][0], img5['re'][0]],
               [0, 0, 0, -img4['re'][0], -img4['re'][1], -1, img4['re'][0]*img5['re'][1], img4['re'][1]*img5['re'][1], img5['re'][1]]])
A3 = np.array([[-img4['n'][0], -img4['n'][1], -1, 0, 0, 0, img4['n'][0]*img5['n'][0], img4['n'][1]*img5['n'][0], img5['n'][0]],
               [0, 0, 0, -img4['n'][0], -img4['n'][1], -1, img4['n'][0]*img5['n'][1], img4['n'][1]*img5['n'][1], img5['n'][1]]])
A4 = np.array([[-img4['ll'][0], -img4['ll'][1], -1, 0, 0, 0, img4['ll'][0]*img5['ll'][0], img4['ll'][1]*img5['ll'][0], img5['ll'][0]],
               [0, 0, 0, -img4['ll'][0], -img4['ll'][1], -1, img4['ll'][0]*img5['ll'][1], img4['ll'][1]*img5['ll'][1], img5['ll'][1]]])
A5 = np.array([[-img4['e'][0], -img4['e'][1], -1, 0, 0, 0, img4['e'][0]*img5['e'][0], img4['e'][1]*img5['e'][0], img5['e'][0]],
               [0, 0, 0, -img4['e'][0], -img4['e'][1], -1, img4['e'][0]*img5['e'][1], img4['e'][1]*img5['e'][1], img5['e'][1]]])
A = np.concatenate([A1, A2, A3, A4, A5], axis=0)

U, s, V = np.linalg.svd(A, full_matrices=True)
h = V[8,:]
H = h.reshape(3,3)

image1 = cv2.imread("img4.jpg")
image2 = cv2.imread("img5.jpg")
syn_image5 = cv2.warpPerspective(image1, H, (1920,1080))
cv2.imshow("syn_image", syn_image5)
cv2.waitKey()
cv2.destroyAllWindows()

####### Save Image #######
cv2.imwrite('syn_img2.jpg',syn_image2)
cv2.imwrite('syn_img3.jpg',syn_image3)
cv2.imwrite('syn_img5.jpg',syn_image5)