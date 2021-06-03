using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioProcessingModuleCs.Media.Dsp.WebRtc;
using AudioProcessingModuleCs.Media;

namespace AudioProcessingModuleCs
{
	class Example
	{
		public Example()
		{
			/*

			var enhancer = new WebRtcFilter(240, 100, new AudioFormat(8000), new AudioFormat(8000),
				true, true, true);

			// todo: call this when you play frame to speakers
			enhancer.RegisterFramePlayed(....);


			// todo: call this when you get data from mic before sending to network				
			enhancer.Write(....); // write signal recorded by microphone
			bool moreFrames;
			do
			{
				short[] cancelBuffer = new short[frameSize]; // contains cancelled audio signal
				if (enhancer.Read(cancelBuffer, out moreFrames))
				{					
					SendToNetwork(cancelBuffer)	;	
				}
			} while (moreFrames);
				
			*/

		}
	}
}
