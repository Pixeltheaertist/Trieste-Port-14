cd ./

find . -name "*.ogg" -type f | while read file; do
    channels=$(ffprobe -v error -select_streams a:0 -show_entries stream=channels -of default=noprint_wrappers=1:nokey=1 "$file")
    if [ "$channels" -gt 1 ]; then
        echo "Converting $file to mono..."
        ffmpeg -i "$file" -ac 1 -y "${file%.ogg}_temp.ogg"
        mv "${file%.ogg}_temp.ogg" "$file"
    fi
done

echo "All done!"
