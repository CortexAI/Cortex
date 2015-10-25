﻿using System;
using System.Threading;
using Cortex.Core.Elements;
using Cortex.Core.Model;
using Cortex.Core.Model.Pins;
using Microsoft.Kinect;

namespace Cortex.Kinect
{
    class Kinect : BaseElement, IDisposable
    {
        private readonly KinectSensor _sensor;
        private Flow _flow;

        private readonly FlowOutputPin _onFramesReadyPin = new FlowOutputPin("Frames ready");
        private readonly DataOutputPin<Frames.ColorFrame> _colorFramePin = new DataOutputPin<Frames.ColorFrame>("Color Frame");
        private readonly DataOutputPin<Frames.DepthFrame> _depthFramePin = new DataOutputPin<Frames.DepthFrame>("Depth Frame");
        private readonly DataOutputPin<Frames.SkeletonFrame> _skeletonFramePin = new DataOutputPin<Frames.SkeletonFrame>("Skeleton Frame");
        private readonly DataOutputPin<KinectSensor> _kinectPin = new DataOutputPin<KinectSensor>("Kinect");
        private readonly ManualResetEvent _activeEvent = new ManualResetEvent(false);

        public Kinect()
        {
            AddInputPin(new FlowInputPin("Start", Start));
            AddInputPin(new FlowInputPin("Stop", Stop));
            
            AddOutputPin(_onFramesReadyPin);
            AddOutputPin(_colorFramePin);
            AddOutputPin(_depthFramePin);
            AddOutputPin(_skeletonFramePin);
            AddOutputPin(_kinectPin);


            if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];
                _kinectPin.Value = _sensor;

                try
                {
                    _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        _sensor.DepthStream.Range = DepthRange.Near;
                        _sensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        _sensor.DepthStream.Range = DepthRange.Default;
                        _sensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    _sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    _sensor.SkeletonStream.Enable();
                    _sensor.AllFramesReady += SensorOnAllFramesReady;
                }
                catch (InvalidOperationException ex)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur, say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                    throw;
                }
            }
            else
                throw new Exception("No kinect sensors detected");
        }

        private void Stop(Flow flow)
        {
            if (_sensor == null)
                throw new Exception("No sensor");

            _sensor.Stop();
            _activeEvent.Set();
        }

        private void Start(Flow flow)
        {
            if (_sensor == null)
                throw new Exception("No sensor");

            if (!_sensor.IsRunning)
            {
                
                _sensor.Start();
            }
            
            _flow = flow;
            _activeEvent.WaitOne();
            _activeEvent.Reset();
        }
        
        private void SensorOnAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            ColorImageFrame colorImageFrame = null;
            DepthImageFrame depthImageFrame = null;
            SkeletonFrame skeletonFrame = null;

            try
            {
                colorImageFrame = e.OpenColorImageFrame();
                depthImageFrame = e.OpenDepthImageFrame();
                skeletonFrame = e.OpenSkeletonFrame();

                if (colorImageFrame == null || depthImageFrame == null || skeletonFrame == null)
                    return;

                _colorFramePin.Value = new Frames.ColorFrame(colorImageFrame);
                _depthFramePin.Value = new Frames.DepthFrame(depthImageFrame);
                _skeletonFramePin.Value = new Frames.SkeletonFrame(skeletonFrame);
                _onFramesReadyPin.Call(_flow);    
            }
            finally
            {
                if (colorImageFrame != null)
                    colorImageFrame.Dispose();

                if (depthImageFrame != null)
                    depthImageFrame.Dispose();

                if (skeletonFrame != null)
                    skeletonFrame.Dispose();
            }
        }

        public void Dispose()
        {
            if (_sensor != null)
            {
                _sensor.AllFramesReady -= SensorOnAllFramesReady;
                _sensor.Stop();
                _sensor.Dispose();
            }
        }
    }
}
