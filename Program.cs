/*
 * Created by SharpDevelop.
 * User: Matyi
 * Date: 2019.11.15.
 * Time: 23:02
 */
using System;
using ScriptPortal.Vegas;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Add_PiP
{
	/// <summary>
	/// This script adds the Picture In Picture effect to every selected event and adjusts the cropping to match the project's aspect ratio
	/// </summary>
	public class EntryPoint{//The usual stuff for a Vegas script, I'll explain it later (no)
		public void FromVegas(Vegas myVegas){
			PlugInNode pipeffect = myVegas.VideoFX.GetChildByName("VEGAS Picture In Picture");//Getting the PiP effetc
			if (pipeffect == null){//if the effect doesn't exists we exit the script with an error message
				MessageBox.Show("You don't have the VEGAS Picture In Picture effect. \n Please install it and try again!");
				return;
			}
			List<VideoEvent> videvents = new List<VideoEvent>();//A list for the selected events
			foreach (Track myTrack in myVegas.Project.Tracks) {//going through every track and every event, adding the selected video events to the list 
				foreach (TrackEvent myEvent in myTrack.Events) {
					if ((myEvent.MediaType == MediaType.Video) && (myEvent.Selected == true)){
						videvents.Add((VideoEvent)myEvent);
					}
				}
			}
			double proWidth = myVegas.Project.Video.Width;//the project's width
			double proHeight = myVegas.Project.Video.Height;//the project's height
			VideoMotionBounds newBound;//variable for the crop's size
			VideoMotionBounds newerBound;//variable for crop size if the first one doesn't fit the whole picture
			foreach (VideoEvent pipevent in videvents) {// for each video event in the list
				Take piptake = pipevent.ActiveTake;//getting the width and height of the event's source
				VideoStream pipstream = piptake.MediaStream as VideoStream;
				int myWidth = pipstream.Width;//the event's width
				int myHeight = pipstream.Height;//the event"s height
				double proAspect = myWidth/(myHeight*(proWidth/proHeight));//calculating the correct number to multiply later the width/height later
				VideoMotionKeyframe reframe = new VideoMotionKeyframe(Timecode.FromFrames(0));//creating a new Pan/Crop keyframe at the beginning of the event
				pipevent.VideoMotion.Keyframes.Add(reframe);
				if (myWidth > myHeight){//calculating the size of the pan/crop keyframe with the help of the previously calculated value (proAspect) (EXTREMLY COMPLEX AND DANGEROUS, handle with care)
					newBound = new VideoMotionBounds(new VideoMotionVertex((float)(reframe.Center.X-(double)(myWidth/2)),(float)(reframe.Center.Y-(double)(myHeight/2)*proAspect)),new VideoMotionVertex((float)(reframe.Center.X+(double)(myWidth/2)),(float)(reframe.Center.Y-(double)(myHeight/2)*proAspect)),new VideoMotionVertex((float)(reframe.Center.X+(double)(myWidth/2)),(float)(reframe.Center.Y+(double)(myHeight/2)*proAspect)),new VideoMotionVertex((float)(reframe.Center.X-(double)(myWidth/2)),(float)(reframe.Center.Y+(double)(myHeight/2)*proAspect)));
					if (Math.Abs(newBound.TopLeft.Y-newBound.BottomLeft.Y) < myHeight){//if the crop is the correct aspect ration, but it still cuts out part of the image, this code will run and make a cropize, which covers the whole picture with the correct ratio (MORE MATH)
						float multiply = myHeight/Math.Abs(newBound.TopLeft.Y-newBound.BottomLeft.Y);
						float actWidth = Math.Abs(newBound.TopRight.X-newBound.TopLeft.X)/2;
						float toHeight = myHeight/2;
						newerBound = new VideoMotionBounds(new VideoMotionVertex(reframe.Center.X-actWidth*multiply,reframe.Center.Y-toHeight),new VideoMotionVertex(reframe.Center.X+actWidth*multiply,reframe.Center.Y-toHeight),new VideoMotionVertex(reframe.Center.X+actWidth*multiply,reframe.Center.Y+toHeight),new VideoMotionVertex(reframe.Center.X-actWidth*multiply,reframe.Center.Y+toHeight));
						newBound = newerBound;
					}
				}
				else{//almost same as above, casual math
					newBound = new VideoMotionBounds(new VideoMotionVertex((float)(reframe.Center.X-(double)(myWidth/2)/proAspect),(float)(reframe.Center.Y-(double)(myHeight/2))),new VideoMotionVertex((float)(reframe.Center.X+(double)(myWidth/2)/proAspect),(float)(reframe.Center.Y-(double)(myHeight/2))),new VideoMotionVertex((float)(reframe.Center.X+(double)(myWidth/2)/proAspect),(float)(reframe.Center.Y+(double)(myHeight/2))),new VideoMotionVertex((float)(reframe.Center.X-(double)(myWidth/2)/proAspect),(float)(reframe.Center.Y+(double)(myHeight/2))));
					if (Math.Abs(newBound.TopRight.X-newBound.TopLeft.X) < myWidth){
						float multiply = myHeight/Math.Abs(newBound.TopRight.X-newBound.TopLeft.X);
						float toWidth = myWidth/2;
						float actHeight = Math.Abs(newBound.TopLeft.Y-newBound.BottomLeft.Y/2);
						newerBound = new VideoMotionBounds(new VideoMotionVertex(reframe.Center.X-toWidth,reframe.Center.Y-actHeight*multiply),new VideoMotionVertex(reframe.Center.X+toWidth,reframe.Center.Y-actHeight*multiply),new VideoMotionVertex(reframe.Center.X+toWidth,reframe.Center.Y+actHeight*multiply),new VideoMotionVertex(reframe.Center.X-toWidth,reframe.Center.Y+actHeight*multiply));
						newBound = newerBound;
					}
				}
				reframe.Bounds = newBound;//setting the keyframe's size
				pipevent.Effects.AddEffect(pipeffect);//adding the PiP effect to the event
			}
		}
	}
}