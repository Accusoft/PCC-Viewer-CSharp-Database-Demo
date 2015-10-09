`/Cache` folder
=============
This will be accessed temporarily for caching purposes.
In the PCC viewer samples, the cache folder used by the web tier(s) is set in the
pcc.config files. Specifically, it is set with the XML element `<TempcachePath>`. The web
tier may read and write to this folder.

*Note: Ensure that the web server process can read and write to this directory.*