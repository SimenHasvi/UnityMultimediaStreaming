# UnityMultimediaStreaming

This Unity plugin provides a good framework for enabling audio and video streamin. As of now a Kafka network implementation is included, but thanks to the modular approach new network modules can easily be created and implemented.

## Audio
The plugin provides the ability to sample, stream, and playback audio. With support for multiple streams from multiple users, perfect for voice chat. Included is a audio codec module using the Opus codec, as well as an audio processing module using speexdps.

## Video
Similar to audio, you can sample, stream, and playback video as well, from multiple users.

## TODO
WebRTC for networking and processing.
Performance improvements.
Bug fixes.
Move Kafka implementation over to the more robust Confluent Kafka.

## How to install
To use it, clone this repo to your Unity project, you have to enable unsafe code. Included is an example scene which shows an example implementation of both audio and video streaming.

It's made in Unity 2020.3.7f1, but any recent version should be just fine. It works for Windows, Mac, and Linux. For other platforms you need to compile speexdsp.

[Kafka-net](https://github.com/Jroland/kafka-net) by is used for the Kafka connection, [concentus](https://github.com/lostromb/concentus) is used for the Opus codec, [speexdsp](https://gitlab.xiph.org/xiph/speexdsp) is used for audio processing.
