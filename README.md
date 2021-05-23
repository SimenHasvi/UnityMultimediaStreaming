# UnityVoiceChat

Simple voice chat implementation for Unity using the Opus codec with Kafka.

This simple applications demonstrates a fast and simple voice-chat function for Unity, using the Opus codec. In this implementation a Kafka server is used, but it can easily be modified to use other networking solutions as well.

A VoiceChat GameObject is present in the default scene, this will handle all the voice chat functionalities and micropone input. You can change settings in the inspector as you wish, the most important of which is the id.

The VoiceAudio object will listen for a specific user and play the audio if any. You can select which user id to listen for in the inspector. With multiple users you simply copy this object and set the appropriate id.

This application is simple and uses only managed code, so it should fully be compatible with any platform with no hassle.

Its made in Unity 2020.3.7f1, but any recent version should be just fine.

[Kafka-net](https://github.com/Jroland/kafka-net) by is used for the Kafka connection, and [concentus](https://github.com/lostromb/concentus) is used for the Opus codec.
