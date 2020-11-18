#!/bin/bash
rm Lavalink.old.jar
mv Lavalink.jar Lavalink.old.jar
curl -s https://api.github.com/repos/Frederikam/Lavalink/releases/latest \
| grep "browser_download_url.*jar" \
| cut -d : -f 2,3 \
| tr -d \" \
| wget -qi -