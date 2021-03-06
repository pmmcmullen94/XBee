﻿using BinarySerialization;

namespace XBee.Frames
{
    internal class TxStatusExtFrame : CommandResponseFrameContent
    {
        public TxStatusExtFrame()
        {
            ShortAddress = ShortAddress.Broadcast;
        }

        [FieldOrder(0)]
        public ShortAddress ShortAddress { get; set; }

        [FieldOrder(1)]
        public byte RetryCount { get; set; }

        [FieldOrder(2)]
        public DeliveryStatusExt DeliveryStatus { get; set; }

        [FieldOrder(3)]
        public DiscoveryStatus DiscoveryStatus { get; set; }
    }
}
