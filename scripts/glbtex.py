#!/usr/bin/env python3
"""
Replace embedded images of a GLB by image name (geometry untouched, bufferView indices kept).
Usage: glbtex.py in.glb out.glb name=file.jpg [name=file.png ...] [--shrink name,name]  (--shrink: 4x4 grey placeholder)
"""
import io, json, struct, sys
from PIL import Image

def read(path):
    b = open(path, 'rb').read()
    assert b[:4] == b'glTF'
    off = 12; j = None; bin_ = b''
    while off + 8 <= len(b):
        cl, ct = struct.unpack('<II', b[off:off + 8])
        if ct == 0x4E4F534A: j = json.loads(b[off + 8:off + 8 + cl])
        elif ct == 0x004E4942: bin_ = b[off + 8:off + 8 + cl]
        off += 8 + cl
    return j, bin_

def write(path, j, bin_):
    bin_ = bytes(bin_)
    while len(bin_) % 4: bin_ += b'\0'
    js = json.dumps(j, separators=(',', ':')).encode()
    while len(js) % 4: js += b' '
    out = struct.pack('<III', 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(bin_))
    out += struct.pack('<II', len(js), 0x4E4F534A) + js + struct.pack('<II', len(bin_), 0x004E4942) + bin_
    open(path, 'wb').write(out)

def swap(src, dst, mapping, shrink=()):
    j, bin_ = read(src)
    replace = {}
    for name, path in mapping.items():
        data = open(path, 'rb').read()
        mime = 'image/jpeg' if path.lower().endswith(('.jpg', '.jpeg')) else 'image/png'
        replace[name] = (data, mime)
    for name in shrink:
        buf = io.BytesIO(); Image.new('RGB', (4, 4), (128, 128, 128)).save(buf, 'PNG')
        replace[name] = (buf.getvalue(), 'image/png')
    view_of_image = {}
    for img in j['images']:
        if img.get('name') in replace:
            view_of_image[img['bufferView']] = img['name']
    new_bin = bytearray(); done = set()
    for vi, v in enumerate(j['bufferViews']):
        while len(new_bin) % 4: new_bin.append(0)
        if vi in view_of_image:
            data, mime = replace[view_of_image[vi]]
            for img in j['images']:
                if img.get('name') == view_of_image[vi]: img['mimeType'] = mime
            done.add(view_of_image[vi])
        else:
            data = bin_[v.get('byteOffset', 0):v.get('byteOffset', 0) + v['byteLength']]
        v['byteOffset'] = len(new_bin); v['byteLength'] = len(data)
        new_bin += data
    j['buffers'] = [dict(byteLength=len(new_bin))]
    j.setdefault('asset', {})['generator'] = (j.get('asset', {}).get('generator', '') + ' +texforge-2.1').strip()
    write(dst, j, new_bin)
    missing = set(replace) - done
    print('%s -> %s: replaced %d images%s, %.2f MB' % (src, dst, len(done), (' (missing: %s)' % missing) if missing else '', len(new_bin) / 1e6))

if __name__ == '__main__':
    src, dst = sys.argv[1], sys.argv[2]
    mapping = {}; shrink = []
    args = sys.argv[3:]
    i = 0
    while i < len(args):
        if args[i] == '--shrink': shrink = args[i + 1].split(','); i += 2; continue
        k, v = args[i].split('=', 1); mapping[k] = v; i += 1
    swap(src, dst, mapping, shrink)
