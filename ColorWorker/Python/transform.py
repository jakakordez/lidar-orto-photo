import math


# http://www2.arnes.si/~gljsentvid10/krugeoz3.htm
# http://www2.arnes.si/~gljsentvid10/ge_gau2.htm


class Transform:

    @staticmethod
    def gps_dd_to_dms(dd, round_decimals=4):
        mnt, sec = divmod(dd * 3600, 60)
        deg, mnt = divmod(mnt, 60)
        return round(deg, round_decimals), round(mnt, round_decimals), round(sec, round_decimals)

    @staticmethod
    def gps_dms_to_dd(dms):
        return 1 * dms[0] + (dms[1] * 60 + 1 * dms[2]) / 3600

    @staticmethod
    def gk_to_pixel(y, x, map, zoom):
        level = map.get_zoom_level(zoom)
        if level is None:
            return None

        # Get rid of the GK zone number
        gk_ym = y - 5000000
        gk_xm = x - 5000000

        tiles_xm = gk_xm - map.origin_x
        tiles_ym = map.origin_y - gk_ym

        pixels_x = tiles_xm / level.resolution
        pixels_y = tiles_ym / level.resolution

        tile_x = math.floor(pixels_x / map.tile_size_x)
        tile_y = math.floor(pixels_y / map.tile_size_y)

        tile_pixels_x = round(pixels_x % map.tile_size_x)
        tile_pixels_y = round(pixels_y % map.tile_size_y)

        return [tile_x, tile_y, tile_pixels_x, tile_pixels_y]

    @staticmethod
    def gk_to_gps(x, y, h):

        # constants
        wgs84_a = 6378137.0  # m
        wgs84_b = 6356752.314  # m
        wgs84_e2 = 0.00669438006676466  # e^2
        wgs84_e2_ = 0.00673949681993606  # e'^2;

        bessel_a = 6377397.155  # m
        bessel_a2 = 40671194472602.1  # a^2
        bessel_b = 6356078.963  # m
        bessel_b2 = 40399739783891.2
        bessel_e2 = 0.00667437217497493  # e^2
        bessel_e2_ = 0.00671921874158131  # e'^2

        dX = -409.520465
        dY = -72.191827
        dZ = -486.872387
        Alfa = 1.49625622332431e-05
        Beta = 2.65141935723559e-05
        Gama = -5.34282614688910e-05
        dm = -17.919456e-6

        M0 = [1.0, math.sin(Gama), -1 * math.sin(Beta)]
        M1 = [-1 * math.sin(Gama), 1, math.sin(Alfa)]
        M2 = [math.sin(Beta), -math.sin(Alfa), 1]

        E = 4.76916455578838e-12
        D = 3.43836164444015e-9
        C = 2.64094456224583e-6
        B = 0.00252392459157570
        A = 1.00503730599692

        y = (y - 500000) / 0.9999
        x = (1 * x + 5000000) / 0.9999  # 1*x  !!!!!!!!!!!

        ab = (1 * bessel_a + 1 * bessel_b)
        fi0 = (2 * x) / ab

        dif = 1.0
        p1 = bessel_a * (1 - bessel_e2)
        n = 25
        while (abs(dif) > 0) and (n > 0):
            L = p1 * (A * fi0 - B * math.sin(2 * fi0) + C * math.sin(4 * fi0) - D * math.sin(6 * fi0) + E *
                      math.sin(8 * fi0))
            dif = (2 * (x - L) / ab)
            fi0 = fi0 + dif
            n -= 1

        N = bessel_a / (math.sqrt(1 - bessel_e2 * math.pow(math.sin(fi0), 2)))
        t = math.tan(fi0)
        t2 = math.pow(t, 2)
        t4 = math.pow(t2, 2)
        cosFi = math.cos(fi0)
        ni2 = bessel_e2_ * math.pow(cosFi, 2)
        lmd = 0.261799387799149 + \
              (y / (N * cosFi)) - \
              (((1 + 2 * t2 + ni2) * pow(y, 3)) / (6 * pow(N, 3) * cosFi)) + \
              (((5 + 28 * t2 + 24 * t4) * pow(y, 5)) / (120 * pow(N, 5) * cosFi))

        fi = fi0 - ((t * (1 + ni2) * pow(y, 2)) / (2 * pow(N, 2))) + \
             (t * (5 + 3 * t2 + 6 * ni2 - 6 * ni2 * t2) * pow(y, 4)) / (24 * pow(N, 4)) - \
             (t * (61 + 90 * t2 + 45 * t4) * pow(y, 6)) / (720 * pow(N, 6))

        N = bessel_a / (math.sqrt(1 - bessel_e2 * pow(math.sin(fi), 2)))
        X = (N + h) * math.cos(fi) * math.cos(lmd)
        Y = (N + h) * math.cos(fi) * math.sin(lmd)
        Z = ((bessel_b2 / bessel_a2) * N + h) * math.sin(fi)

        X -= dX
        Y -= dY
        Z -= dZ
        X /= (1 + dm)
        Y /= (1 + dm)
        Z /= (1 + dm)

        X1 = X - M0[1] * Y - M0[2] * Z
        Y1 = -1 * M1[0] * X + Y - M1[2] * Z
        Z1 = -1 * M2[0] * X - M2[1] * Y + Z

        p = math.sqrt(math.pow(X1, 2) + math.pow(Y1, 2))
        O = math.atan2(Z1 * wgs84_a, p * wgs84_b)
        SinO = math.sin(O)
        Sin3O = math.pow(SinO, 3)
        CosO = math.cos(O)
        Cos3O = math.pow(CosO, 3)

        fif = math.atan2(Z1 + wgs84_e2_ * wgs84_b * Sin3O, p - wgs84_e2 * wgs84_a * Cos3O)
        lambdaf = math.atan2(Y1, X1)

        N = wgs84_a / math.sqrt(1 - wgs84_e2 * math.pow(math.sin(fif), 2))
        hf = p / math.cos(fif) - N

        fif = (fif * 180) / math.pi
        lambdaf = (lambdaf * 180) / math.pi

        return [fif, lambdaf, hf]

    @staticmethod
    def gps_to_gk(lat, lon):
        FI = lat
        LA = lon

        if isinstance(lat, tuple):
            FI = Transform.gps_dms_to_dd(lat)
        if isinstance(lon, tuple):
            LA = Transform.gps_dms_to_dd(lon)

        CONA = 5
        B1 = 0.9999
        B2 = 500000.0
        B3 = 1000000.0

        AA = 6377397.155
        BB = 6356078.962818
        EE = (AA * AA - BB * BB) / (BB * BB)
        PI = 4.0 * math.atan(1.0)
        E2 = (AA * AA - BB * BB) / (AA * AA)
        E4 = E2 * E2
        E6 = E4 * E2
        E8 = E4 * E4
        E10 = E6 * E4
        A = 1.0 + 3.0 * E2 / 4.0 + 45.0 * E4 / 64.0 + 175.0 * E6 / 256.0 + 11025.0 * E8 / 16384.0 + 43659.0 * E10 / 65536.0
        B = 3.0 * E2 / 4.0 + 15.0 * E4 / 16.0 + 525.0 * E6 / 512.0 + 2205.0 * E8 / 2048.0 + 72765.0 * E10 / 65536.0
        C = 15.0 * E4 / 64.0 + 105.0 * E6 / 256.0 + 2205.0 * E8 / 4096.0 + 10395.0 * E10 / 16384.0
        D = 35.0 * E6 / 512.0 + 315.0 * E8 / 2048.0 + 31185.0 * E10 / 131072.0
        E = 315.0 * E8 / 16384.0 + 3465.0 * E10 / 65536.0
        F = 693.0 * E10 / 131072.0

        FI = FI * PI / 180
        LA = LA * PI / 180
        T = math.sin(FI) / math.cos(FI)
        A1 = AA * AA / math.sqrt(AA * AA + BB * BB * (T * T))
        A2 = A1 * math.sin(FI) / 2.0
        A3 = A1 * (math.cos(FI) * math.cos(FI)) / 6.0 * (1.0 - T * T + EE * math.cos(FI) * math.cos(FI))
        A4 = A1 * math.sin(FI) * (math.cos(FI) * math.cos(FI)) / 24.0 * \
             (5.0 - T * T + 9.0 * EE * (math.cos(FI) * math.cos(FI)))
        A5 = A1 * math.sin(FI) * math.cos(FI) * math.cos(FI) * math.cos(FI) * math.cos(FI) / 120.0 * (
                5.0 - 18.0 * T * T + (T * T * T * T) + EE * (14.0 - 72.0 * (math.sin(FI) * math.sin(FI))))

        A6 = A1 * math.sin(FI) * math.cos(FI) * math.cos(FI) * math.cos(FI) * math.cos(FI) / 720.0 * \
             (61.0 - 58.0 * (T * T) + (T * T * T * T))
        LAM = LA - CONA * 3.0 * PI / 180.0
        XX = AA * (1.0 - E2) * \
             (A * FI - B / 2.0 * math.sin(2.0 * FI) + C / 4.0 * math.sin(4.0 * FI) - D / 6.0 * math.sin(
                 6.0 * FI) + E / 8.0 * math.sin(8.0 * FI) - F / 10.0 * math.sin(10.0 * FI))

        XG = 1 * XX + A2 * (LAM * LAM) + A4 * LAM * LAM * LAM * LAM + A6 * (LAM * LAM * LAM * LAM) * (LAM * LAM)
        YG = A1 * LAM + A3 * (LAM * LAM) * LAM + A5 * (LAM * LAM * LAM * LAM) * LAM
        y = YG * B1 + 1 * B2 + CONA * B3
        x = XG * B1
        ix = round(x * 1000) / 1000
        iy = round(y * 1000) / 1000

        return [ix, iy]

    def wgs84_to_gk(lat, lon, H):
        fi = lat * math.pi / 180
        lam = lon * math.pi / 180

        # constants
        wgs84_a = 6378137.0  # m
        wgs84_a2 = 40680631590769
        wgs84_b2 = 40408299981544.4
        wgs84_e2 = 0.00669438006676466  # e ^ 2

        bessel_a = 6377397.155  # m
        bessel_b = 6356078.963  # m
        bessel_e2 = 0.00667437217497493  # e ^ 2
        bessel_e2_ = 0.00671921874158131  # e'^2

        dX = -409.520465
        dY = -72.191827
        dZ = -486.872387
        Alfa = 1.49625622332431e-05
        Beta = 2.65141935723559e-05
        Gama = -5.34282614688910e-05
        dm = -17.919456e-6

        M0 = [1.0, math.sin(Gama), -1 * math.sin(Beta)]
        M1 = [-1 * math.sin(Gama), 1, math.sin(Alfa)]
        M2 = [math.sin(Beta), -math.sin(Alfa), 1]

        E = 4.76916455578838e-12
        D = 3.43836164444015e-9
        C = 2.64094456224583e-6
        B = 0.00252392459157570
        A = 1.00503730599692

        N = wgs84_a / (math.sqrt(1 - wgs84_e2 * math.pow(math.sin(fi), 2)))
        X = (N + H) * math.cos(fi) * math.cos(lam)
        Y = (N + H) * math.cos(fi) * math.sin(lam)
        Z = ((wgs84_b2 / wgs84_a2) * N + H) * math.sin(fi)

        X1 = X + M0[1] * Y + M0[2] * Z
        Y1 = M1[0] * X + Y + M1[2] * Z
        Z1 = M2[0] * X + M2[1] * Y + Z

        X = (X1 * (1 + dm)) + dX
        Y = (Y1 * (1 + dm)) + dY
        Z = (Z1 * (1 + dm)) + dZ

        p = math.sqrt(math.pow(X, 2) + math.pow(Y, 2))
        O = math.atan2(Z * bessel_a, p * bessel_b)
        SinO = math.sin(O)
        Sin3O = math.pow(SinO, 3)
        CosO = math.cos(O)
        Cos3O = math.pow(CosO, 3)
        fi = math.atan2(Z + bessel_e2_ * bessel_b * Sin3O, p - bessel_e2 * bessel_a * Cos3O)
        lam = math.atan2(Y, X)
        N = bessel_a / math.sqrt(1 - bessel_e2 * math.pow(math.sin(fi), 2))

        m0 = 0.9999
        SinFI = math.sin(fi)
        CosFI = math.cos(fi)
        Cos2FI = math.pow(CosFI, 2)
        Cos3FI = math.pow(CosFI, 3)
        Cos5FI = math.pow(CosFI, 5)
        c = bessel_e2_ * Cos2FI
        c2 = math.pow(c, 2)

        l = lam - 0.261799387799149
        l2 = math.pow(l, 2)
        l3 = math.pow(l, 3)
        l4 = math.pow(l, 4)
        l5 = math.pow(l, 5)
        l6 = math.pow(l, 6)

        L = bessel_a * (1 - bessel_e2) * (A * fi - (B * math.sin(2 * fi)) + (C * math.sin(4 * fi)) -
                                          (D * math.sin(6 * fi)) + (E * math.sin(8 * fi)))
        T = math.pow(math.tan(fi), 2)
        T2 = math.pow(T, 2)

        X = L + (l2 * N * SinFI * CosFI / 2) + (l4 * N * SinFI * Cos3FI * (5 - T + 9 * c + 4 * c2) / 24) + \
            (l6 * N * SinFI * Cos5FI * (61 - 58 * T + T2 + 600 * c - 330 * bessel_e2_) / 720)
        X *= m0
        X -= 5000000

        Y = (l * N * CosFI) + (l3 * N * Cos3FI * (1 - T + c) / 6) + \
            (l5 * N * Cos5FI * (5 - 18 * T + T2 + 72 * c - 58 * bessel_e2_) / 120)
        Y *= m0
        Y += 500000

        return [round(1 * 5000000 + 1 * X), round(1 * 5000000 + Y)]
