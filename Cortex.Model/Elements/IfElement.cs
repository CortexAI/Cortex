﻿using System;
using System.ComponentModel.Composition;

namespace Cortex.Model.Elements
{
    [Serializable]
    [Export(typeof(IElement))]
    public class IfElement : IElement
    {
        public string Name { get { return "If"; } }
        public string Category { get { return "Common"; } }
        public Uri IconUri { get { return null; } }
        public string Description { get { return "If statement"; } }
        public InputPin[] Inputs { get; private set; }
        public OutputPin[] Outputs { get; private set; }

        public IfElement()
        {
            Inputs = new[]
            {
                new FlowInputPin(OnCall),
                new InputPin("Condition", typeof (bool), false),
            };

            Outputs = new OutputPin[]
            {
                new FlowOutputPin("True"),
                new FlowOutputPin("False"),
            };
        }

        public void OnCall()
        {
            if((bool)Inputs[1].Value)
                ((FlowOutputPin)Outputs[0]).Call();
            else
                ((FlowOutputPin)Outputs[1]).Call();
        }
    }
}