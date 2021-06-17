# UnityVoiceChat

Simple voice chat implementation for Unity using the Opus codec with Kafka.

This simple plugin demonstrates a fast and simple voice-chat function for Unity, using the Opus codec. In this implementation a Kafka server is used, but it can easily be modified to use other networking solutions as well.

To use it, clone this repo to your Unity project, Then add the VoiceChat script to a GameObject. You can now fill in the nessesary settings there.
You also need to allow unsafe code in the player settings.


Its made in Unity 2020.3.7f1, but any recent version should be just fine. It works for Windows, Mac, and Linux. For other platforms you need to compile speexdsp.

[Kafka-net](https://github.com/Jroland/kafka-net) by is used for the Kafka connection, [concentus](https://github.com/lostromb/concentus) is used for the Opus codec, [speexdsp](https://gitlab.xiph.org/xiph/speexdsp) is used for audio processing.
