acinerella: acinerella.c acinerella.h
	gcc -c -fPIC acinerella.c -I /usr/local/include $(shell pkg-config --cflags libavformat 2>/dev/null)

ifeq ($(shell uname),Linux)
	gcc -shared -o libacinerella.so acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale -lswresample
	strip libacinerella.so
	cp libacinerella.so ../../../../Output/
else ifeq ($(shell uname),Darwin)
	gcc -shared -o libacinerella.dylib acinerella.o $(shell pkg-config --libs libavformat libavcodec libavutil libswscale libswresample)
	cp libacinerella.dylib ../../../../Output/
else
	gcc -shared -o acinerella.dll -fPIC acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale -lws2_32
	strip acinerella.dll
endif

clean:
	rm -f libacinerella.so ../../../../Output/libacinerella.so libacinerella.dylib ../../../../Output/libacinerella.dylib