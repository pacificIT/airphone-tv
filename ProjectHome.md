The goal of this project is to create a set of tools that enable developers to leverage Apple's AirTunes technology in their homes to expand the usefulness of their existing HTPC / media server type systems.

Initial target applications are an AirTunes control program for the iPhone which will control a single generic AirTunes server (e.g., axStream, which copies and transmits all audio output to the default audio device).

The specific use case I am targetting is being able to control volume and turn speakers on/off for streaming Pandora radio through Boxee. A future project may be a similar AirTunes control panel as a Boxee plugin.

Thanks goes out to ovensen, whose code base I used to start this project.  The original basis for this project built by him is at http://axstream.codeplex.com/

Also to [Mark Heath](http://mark-dot-net.blogspot.com/) who built [NAudio](http://www.codeplex.com/naudio), which I have used to replace the overly simplistic and out-dated cswavrec.

Finally, to DVD Jon - Jon Lech Johansen - who extracted the AirTunes public key and wrote [JustePort](http://nanocr.eu/software/justeport/), which was used by ovesen in his original axStream app and is still being used in this project