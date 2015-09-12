# reNX [![Build Status](https://travis-ci.org/angelsl/ms-reNX.svg?branch=master)](https://travis-ci.org/angelsl/ms-reNX) [![Build status](https://ci.appveyor.com/api/projects/status/j4xok1y5d45itkrk/branch/master?svg=true)](https://ci.appveyor.com/project/angelsl/ms-renx)
...is a simple and clean .NET library for reading NX files.

## Usage
Add a reference to reNX, and then load an NX file like so:

    NXFile nx = new NXFile("PKG4.nx");

Then access whatever nodes you need:

    Bitmap m = nx.ResolvePath("Effect/BasicEff.img/LevelUp/7").ValueOrDie<Bitmap>();
    Bitmap m = ((NXValuedNode<Bitmap>)nx["Effect"]["BasicEff.img"]["LevelUp"]["7"]).Value;

If you have any questions, feel free to ask. Do consult the XMLdoc as reNX is pretty well documented.

## License
reNX is licensed under the GNU GPL v3.0 with Classpath Exception. Please read the file header carefully!

    reNX is copyright angelsl, 2011 to 2015 inclusive.

    reNX is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    reNX is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with reNX. If not, see <http://www.gnu.org/licenses/>.

    Linking this library statically or dynamically with other modules
    is making a combined work based on this library. Thus, the terms and
    conditions of the GNU General Public License cover the whole combination.

    As a special exception, the copyright holders of this library give you
    permission to link this library with independent modules to produce an
    executable, regardless of the license terms of these independent modules,
    and to copy and distribute the resulting executable under terms of your
    choice, provided that you also meet, for each linked independent module,
    the terms and conditions of the license of that module. An independent
    module is a module which is not derived from or based on this library.

## Building LZ4 for Linux
As different systems may have different versions of GCC/libc and the like, it is not feasible to bundle a compiled version of LZ4 for Linux.

Instead, use the included makefile (`LZ4Makefile`) to compile LZ4 on Linux. E.g.

    svn checkout http://lz4.googlecode.com/svn/trunk/ lz4
    cd lz4
    cp -f ../LZ4Makefile Makefile
    make
    
Then copy the .so file, either `liblz4_32.so` or `liblz4_64.so`, depending on your architecture, to the same directory as your application.

## Acknowledgements
 * [retep998](https://github.com/retep998), the co-designer of the NX format
 * Cedric, [aatxe](https://github.com/aatxe) and others from [#vana](irc://irc.fyrechat.net/vana) for their suggestions and help