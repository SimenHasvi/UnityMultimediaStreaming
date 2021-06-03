# UnityVoiceChat

Simple voice chat implementation for Unity using the Opus codec with Kafka.

This simple applications demonstrates a fast and simple voice-chat function for Unity, using the Opus codec. In this implementation a Kafka server is used, but it can easily be modified to use other networking solutions as well.

This application is simple and uses only managed code, so it should fully be compatible with any platform with no hassle.

Its made in Unity 2020.3.7f1, but any recent version should be just fine.

[Kafka-net](https://github.com/Jroland/kafka-net) by is used for the Kafka connection, [concentus](https://github.com/lostromb/concentus) is used for the Opus codec, and [the following WebRTC C#-port](http://startrinity.com/OpenSource/Aec/AecVadNoiseSuppressionLibrary.aspx) is used for aec.
