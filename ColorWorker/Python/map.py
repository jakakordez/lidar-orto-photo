import time, json


from transform import Transform
from PIL import Image
import urllib.request
import math, os


class MapLevel:
    def __init__(self, start_tile=[], end_tile=[], scale=0, resolution=0):
        self.start_tile = start_tile
        self.end_tile = end_tile
        self.scale = scale
        self.resolution = resolution


class Map:
    levels = []

    image_url = None
    cache_path = None

    map_dir = None

    def __init__(self, origin_x=0, origin_y=0, tile_size_x=256, tile_size_y=256):
        self.origin_x = origin_x
        self.origin_y = origin_y
        self.tile_size_x = tile_size_x
        self.tile_size_y = tile_size_y

    def add_level(self, level):
        self.levels.append(level)

    @staticmethod
    def load_from_json(json_file):
        try:
            with open(json_file) as fd:
                data = json.load(fd)
        except Exception as e:
            print('Error reading json file: %s' % e)
            return None

        try:
            nm = Map()
            nm.image_url = data['imageUrl']
            nm.cache_path = data['cachePath']
            nm.tile_size_x = data['tileInfo']['rows']
            nm.tile_size_y = data['tileInfo']['cols']
            nm.origin_x = data['tileInfo']['origin']['x']
            nm.origin_y = data['tileInfo']['origin']['y']
            for lod in data['tileInfo']['lods']:
                map_level = MapLevel()
                map_level.resolution = lod['resolution']
                map_level.scale = lod['scale']
                map_level.start_tile = lod['startTile']
                map_level.start_tile = lod['endTile']
                nm.add_level(map_level)
        except KeyError as e:
            print('Error reading json file: missing key %s' % e)
            return None

        return nm

    def get_zoom_level(self, zoom):
        if (zoom < 0) or (zoom > len(self.levels)):
            return None
        return self.levels[zoom]

    def get_pixel_size(self, zoom):
        level = self.get_zoom_level(zoom)
        if level is None:
            return None

        width = level.tile_end[0] - level.title_start[0] + 1
        height = level.tile_end[1] - level.title_start[1] + 1
        return [width, height]

    def generate_tile_url(self, zoom, x, y):
        return self.image_url \
            .replace('{level}', str(zoom))\
            .replace('{column}', str(x))\
            .replace('{row}', str(y))

    def generate_image_path(self, zoom, x, y):
        return self.cache_path\
            .replace('{map-dir}', self.map_dir)\
            .replace('{level}', str(zoom))\
            .replace('{column}', str(x))\
            .replace('{row}', str(y))\
            .replace('\\', '/')
            #.replace('/', '\\')


class BoundingBox:
    def __init__(self, lat1, lon1, lat2, lon2, coor_system='gps'):
        self.coor_system = coor_system
        self.min_lat = min(lat1, lat2)
        self.min_lon = min(lon1, lon2)
        self.max_lat = max(lat1, lat2)
        self.max_lon = max(lon1, lon2)

    def get_min_latlon(self):
        return self.min_lat, self.min_lon

    def get_max_latlon(self):
        return self.max_lat, self.max_lon


class MapDownloader:
    def __init__(self, map):
        self.map = map
        self.delay = 0.01  # delay between two requests

    def download_tiles(self, zoom):
        level = self.map.get_zoom_level(zoom)

        for x in range(level.start_tile[1], level.end_tile[1] + 1):
            for y in range(level.start_tile[0], level.end_tile[0] + 1):
                url_image = self.map.generate_tile_url(zoom, x, y)
                file_out = self.map.generate_image_path(zoom, x, y)

                directory = os.path.dirname(file_out)
                if not os.path.exists(directory):
                    os.makedirs(directory)

                if os.path.isfile(file_out):
                    print("File already exists %d/%d/%d" % (zoom, y, x))
                else:
                    print("Downloading %s" % url_image)
                    try:
                        opener = urllib.request.build_opener()
                        opener.addheaders = [('User-agent', 'Mozilla/5.0')]
                        urllib.request.install_opener(opener)
                        urllib.request.urlretrieve(url_image, file_out)
                    except urllib.error.HTTPError as e:
                        if e == 404:
                            print('Error: tile not found (%d)' % e.code)
                        else:
                            print('Error: http code %d' % e.code)

                    if self.delay is not None:
                        time.sleep(self.delay)
        print("Done")


class MapExporter:
    def __init__(self, map):
        self.map = map
        self.bbox = None
        self.download = False
        self.verbose = False

    def set_bbox(self, lat1, lon1, lat2, lon2, coor_system):
        self.bbox = BoundingBox(lat1, lon1, lat2, lon2, coor_system)

    def download_tile(self, zoom, x, y):
        url_image = self.map.generate_tile_url(zoom, x, y)
        tile_path = self.map.generate_image_path(zoom, x, y)
        print("tile_path %s" % tile_path)
        print("url_image %s" % url_image)
        directory = os.path.dirname(tile_path)
        if not os.path.exists(directory):
            os.makedirs(directory)

        if not os.path.isfile(tile_path):
            if self.verbose:
                print("Downloading %s" % url_image)
            try:
                opener = urllib.request.build_opener()
                opener.addheaders = [('User-agent', 'Mozilla/5.0')]
                urllib.request.install_opener(opener)
                urllib.request.urlretrieve(url_image, tile_path)
            except urllib.error.HTTPError as e:
                if e == 404:
                    print('Error: tile not found (%d)' % e.code)
                else:
                    print('Error: http code %d' % e.code)

    def export_map_tiles(self, start_x, start_y, end_x, end_y, zoom, padding, out_path, out_format,
                         image_width, image_height):

        x_tiles = (end_x - start_x) + 1
        y_tiles = (end_y - start_y) + 1

        width = x_tiles * self.map.tile_size_x
        height = y_tiles * self.map.tile_size_y

        offset_x = offset_y = 0
        if padding is not None:
            width -= (padding[0] + padding[2])
            height -= (padding[1] + padding[3])
            offset_x = padding[0]
            offset_y = padding[1]

        out_img = Image.new('RGB', (width, height))

        for x in range(start_x, end_x + 1):
            for y in range(start_y, end_y + 1):
                image_path = self.map.generate_image_path(zoom, x, y)

                pos_x = (x - start_x) * self.map.tile_size_x - offset_x
                pos_y = (y - start_y) * self.map.tile_size_y - offset_y

                left = top = right = bottom = 0
                if padding is not None:
                    if x == start_x:
                        left = padding[0]
                    if x == end_x:
                        right = padding[2]
                    if y == start_y:
                        top = padding[1]
                    if y == end_y:
                        bottom = padding[3]

                # download file if not exists
                if not os.path.isfile(image_path):
                    self.download_tile(zoom, x, y)

                if os.path.isfile(image_path):
                    tile_img = Image.open(image_path)
                    if (left + right + top + bottom) > 0:
                        tile_img = tile_img.crop((left, top, self.map.tile_size_x - right, self.map.tile_size_y - bottom))
                    out_img.paste(tile_img, (pos_x + left, pos_y + top))
                    tile_img.close()

        if width > image_width:
            w_percent = float(image_width) / float(width)
            width = image_width
            height = math.floor(float(height) * w_percent)

            if height > image_height:
                h_percent = float(image_height) / float(height)
                width = math.floor(float(width) * h_percent)
                height = image_height

            out_img = out_img.resize((width, height))

        out_img.save(out_path, out_format)

    def get_pixels_size(self, zoom, gk1, gk2):
        pixels1 = Transform.gk_to_pixel(gk1[0], gk1[1], self.map, zoom)
        pixels2 = Transform.gk_to_pixel(gk2[0], gk2[1], self.map, zoom)

        x_tiles = (pixels2[0] - pixels1[0]) + 1
        y_tiles = (pixels1[1] - pixels2[1]) + 1

        width = (x_tiles * self.map.tile_size_x) - pixels1[2] - (self.map.tile_size_x - pixels2[2])
        height = (y_tiles * self.map.tile_size_y) - pixels2[3] - (self.map.tile_size_y - pixels1[3])

        return width, height

    def export_image(self, out_path, out_format='PNG', image_width=2000, image_height=2000):
        if self.map is None:
            return None

        if self.bbox.coor_system.lower() == 'gps':
            # transform to Gauss-Krueger
            gk1 = Transform.wgs84_to_gk(self.bbox.min_lat, self.bbox.min_lon, 0)  # left bottom
            gk2 = Transform.wgs84_to_gk(self.bbox.max_lat, self.bbox.max_lon, 0)  # right top
        else:
            gk1 = [self.bbox.min_lat, self.bbox.min_lon]
            gk2 = [self.bbox.max_lat, self.bbox.max_lon]

        zoom = len(self.map.levels) - 1
        for z in range(0, len(self.map.levels)):
            width, height = self.get_pixels_size(z, gk1, gk2)
            # print(width, height)
            if width >= image_width or height >= image_height:
                zoom = z
                break

        pixels1 = Transform.gk_to_pixel(gk1[0], gk1[1], self.map, zoom)
        pixels2 = Transform.gk_to_pixel(gk2[0], gk2[1], self.map, zoom)

        # tile range
        start_x = pixels1[0]
        start_y = pixels2[1]
        end_x = pixels2[0]
        end_y = pixels1[1]

        # padding: left, top, right, bottom
        padding = (pixels1[2], pixels2[3], self.map.tile_size_x - pixels2[2], self.map.tile_size_y - pixels1[3])

        if self.verbose:
            if self.bbox.coor_system.lower() == 'gps':
                print('Bounding box (WGS84): %f, %f, %f, %f' %
                      (self.bbox.min_lat, self.bbox.min_lon, self.bbox.max_lat, self.bbox.max_lon))

            print('Bounding box (GK): %d, %d, %d, %d' %  # as integer !!
                  (gk1[0], gk1[1],  gk2[0], gk2[1]))

            print('Output image: %s (%s)' % (out_path, out_format))
            print('Max image size: %dx%d' % (image_width, image_height))
            print('Tiles directory: %s' % self.map.map_dir)
            print('Tiles range: %d, %d, %d, %d (zoom: %d)' % (start_x, start_y, end_x, end_y, zoom))
            print('Padding: %d, %d, %d, %d' % padding)

        start_time = time.time()
        self.export_map_tiles(start_x, start_y, end_x, end_y, zoom, padding, out_path, out_format, image_width, image_height)
        elapsed_time = time.time() - start_time

        if self.verbose:
            print('Done, image exported in %0.3f seconds\n' % elapsed_time)
