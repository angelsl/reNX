# reNX
...is a simple and clean .NET library for reading NX files.

It, along with [NoLifeNX](https://github.com/NoLifeDev/NoLifeNx) and [WZ2NX](https://github.com/angelsl/ms-wz2nx), is the reference implementation of the NX format.

reNX officially fully supports Windows and Linux only. All platforms that Mono supports are also supported, but not all features may be functional.

##Usage
Add a reference to reNX, and then load an NX file like so:

    NXFile nx = new NXFile("PKG4.nx");

Then access whatever nodes you need:

    Bitmap m = nx.ResolvePath("Effect/BasicEff.img/LevelUp/7").ValueOrDie<Bitmap>();
    Bitmap m = ((NXValuedNode<Bitmap>)nx["Effect"]["BasicEff.img"]["LevelUp"]["7"]).Value;

If you have any questions, feel free to ask. Do consult the XMLdoc as reNX is pretty well documented.

##License
reNX is licensed under the GNU GPL v3.0 with Classpath Exception.

##Building LZ4 for Linux
As different systems may have different versions of GCC/libc and the like, it is not feasible to bundle a compiled version of LZ4 for Linux.

Instead, use the included makefile (`LZ4Makefile`) to compile LZ4 on Linux. E.g.

    svn checkout http://lz4.googlecode.com/svn/trunk/ lz4
    cd lz4
    cp -f ../LZ4Makefile Makefile
    make
    
Then copy the .so file, either `liblz4_32.so` or `liblz4_64.so`, depending on your architecture, to the same directory as your application.

##Acknowledgements
 * [retep998](https://github.com/retep998), the co-designer of the NX format
 * Cedric, [aaronweiss74](https://github.com/aaronweiss74) and others from [#MapleDev](irc://irc.fyrechat.net/MapleDev) and [#vana](irc://irc.fyrechat.net/vana) for their suggestions and help
 * [LZ4](http://code.google.com/p/lz4/), a fast and speedy compression algorithm used in NX to compress images