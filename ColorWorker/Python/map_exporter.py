import argparse, os
from map import Map, MapExporter


def main():
    parser = argparse.ArgumentParser(description='Map tile exporter.')
    parser.add_argument("left", type=float)
    parser.add_argument("bottom", type=float)
    parser.add_argument("right", type=float)
    parser.add_argument("top", type=float)

    parser.add_argument("--bbox-format", help="format of bounding box coordinates",
                        type=str, choices=['gk', 'gps'], default='gps')

    parser.add_argument("--map-conf", help="map configuration file (json)", required=True)

    parser.add_argument("--map-dir", help="map directory with tiles", required=True)

    parser.add_argument("-o", "--output-path", help="output image path", required=True)

    parser.add_argument("-f", "--output-format", help="output image format",
                        type=str, choices=['png', 'jpeg'], default='png')

    parser.add_argument("--width", help="output image width", type=int, default=2000)

    parser.add_argument("--height", help="output image height", type=int, default=2000)

    parser.add_argument("-v", "--verbose", help="print debug info", action="store_true")

    args = parser.parse_args()

    if not os.path.isfile(args.map_conf):
        print("Map configuration file '%s' not exists" % args.map_conf)
        return

    if args.width > 10000 or args.height > 10000:
        print("Output image size %dx%d is too big" % (args.width, args.height))
        return

    dof = Map.load_from_json(args.map_conf)
    if dof is None:
        return

    dof.map_dir = args.map_dir

    me = MapExporter(dof)
    me.verbose = args.verbose
    me.set_bbox(args.left, args.top, args.right, args.bottom, args.bbox_format)
    me.export_image(args.output_path, args.output_format, args.width, args.height)

    # usage sample:
    #   map_exporter.py -o C:\output.png -f png --width 2000 --height 2000 46.358280 14.082000 46.369520 14.108555
    #       --tiles-dir C:\Maps --map_conf dof_2018.json --verbose


def main_test():
    dof = Map.load_from_json('dof_2016.json')

    if dof is None:
        return

    dof.map_dir = 'H:\\Maps\\DOF_2016_TEST'

    me = MapExporter(dof)
    me.download = True  # download missing tiles on-the-fly
    me.verbose = True   # print some useful info

    # Blejsko jezero
    # http://bboxfinder.com/#46.358280,14.082000,46.369520,14.108555
    me.set_bbox(46.358280, 14.082000, 46.369520, 14.108555, 'gps')
    #me.set_bbox(5135319, 5429727, 5136545, 5431784, 'gk')
    me.export_image('H:\\Maps\\export_blejsko_jezero.png', 'PNG', 2000, 2000)

    # Ljubljana
    # http://bboxfinder.com/#46.007216,14.439812,46.089305,14.585552
    me.set_bbox(46.007216, 14.439812, 46.089305, 14.585552, 'gps')
    #me.set_bbox(5096045, 5456987, 5105099, 5468321, 'gk')
    me.export_image('H:\\Maps\\export_ljubljana.png', 'PNG', 2000, 2000)


if __name__ == "__main__":
    main()

    # NOTE: uncomment and replace with main() for a simple testing (don't forget to change directories above)
    # main_test()


