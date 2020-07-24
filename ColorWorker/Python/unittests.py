import unittest
from transform import Transform
from map import Map, MapLevel


class TestTransformMethods(unittest.TestCase):

    def test_gps_to_gk(self):
        self.assertEqual(Transform.gps_to_gk((45, 38, 2), (15, 12, 58)), [5054397.791, 5516847.02])
        self.assertEqual(Transform.gps_to_gk(45.63388889, 15.21611111), [5054397.791, 5516847.02])

        self.assertEqual(Transform.gps_to_gk(46.362462, 14.090116), [5135742.094, 5429994.658])

    def test_wgs84_to_gk(self):
        #  Example (Prešeren statue, Prešeren Square, Ljubljana)
        self.assertEqual(Transform.wgs84_to_gk(46.051401, 14.506337, 0), [5100921, 5462169])

        # self.assertEqual(Transform.gps_to_gk((46, 3, 5.0436), (14, 30, 22.8132)), [5100921, 5462169])
        # [5100921, 5462169] != [5100889.481, 5461803.128]

        # self.assertEqual(Transform.gps_to_gk(46.051401, 14.506337), [5100921, 5462169])
        # [5100921, 5462169] != [5100889.481, 5461803.128]

        # Sv. Marija, Bled
        self.assertEqual(Transform.wgs84_to_gk(46.362462, 14.090116, 0), [5135777, 5430357])


    def test_gps_dd_to_dms(self):
        self.assertEqual(Transform.gps_dd_to_dms(46.051401), (46.0, 3.0, 5.0436))
        self.assertEqual(Transform.gps_dd_to_dms(14.506337), (14.0, 30.0, 22.8132))

    def test_gps_dms_to_dd(self):
        self.assertEqual(Transform.gps_dms_to_dd((46.0, 3.0, 5.0436)), 46.051401)
        self.assertEqual(Transform.gps_dms_to_dd((14.0, 30.0, 22.8132)), 14.506337)

    def test_gk_to_pixel(self):
        dof = Map(345000, 215000, 256)
        dof.add_level(MapLevel([0, 0], [0, 0], 5000000, 1322.9193125052918))
        dof.add_level(MapLevel([0, 0], [1, 1], 2500000, 661.4596562526459))
        dof.add_level(MapLevel([0, 0], [1, 2], 1500000, 396.87579375158754))
        dof.add_level(MapLevel([0, 0], [2, 4], 1000000, 264.5838625010584))
        dof.add_level(MapLevel([0, 0], [3, 5], 750000, 198.43789687579377))
        dof.add_level(MapLevel([0, 0], [5, 8], 500000, 132.2919312505292))
        dof.add_level(MapLevel([1, 1], [10, 16], 250000, 66.1459656252646))
        dof.add_level(MapLevel([1, 2], [18, 27], 150000, 39.687579375158755))
        dof.add_level(MapLevel([2, 4], [27, 41], 100000, 26.458386250105836))
        dof.add_level(MapLevel([3, 5], [36, 55], 75000, 19.843789687579378))
        dof.add_level(MapLevel([5, 8], [54, 83], 50000, 13.229193125052918))
        dof.add_level(MapLevel([11, 17], [108, 166], 25000, 6.614596562526459))
        dof.add_level(MapLevel([19, 28], [181, 276], 15000, 3.9687579375158752))
        dof.add_level(MapLevel([28, 43], [272, 415], 10000, 2.6458386250105836))
        dof.add_level(MapLevel([57, 86], [544, 830], 5000, 1.3229193125052918))
        dof.add_level(MapLevel([115, 173], [1089, 1661], 2500, 0.6614596562526459))

        # Sv. Marija, Bled
        self.assertEqual(Transform.gk_to_pixel(5100921, 5462169, dof, 15), [691, 673, 241, 178])

        # print(Transform.gk_to_pixel(5135777, 5430357, dof, 15)) # [504, 467, 248, 212]
        # print(Transform.gk_to_pixel(5135742.094, 5429994.658, dof, 15)) # [501, 468, 246, 212]


if __name__ == '__main__':
    unittest.main()