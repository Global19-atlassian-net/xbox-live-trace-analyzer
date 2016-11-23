﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace XboxLiveTrace
{
    internal class XR053Rule : IRule
    {
        private static String DisplayName = "XR053Rule";
        public XR053Rule() : base(DisplayName)
        {
        }

        public override void DeserializeJson(JObject json)
        {
        }

        public override RuleResult Run(IEnumerable<ServiceCallItem> items, ServiceCallStats stats)
        {
            bool mpRoundStartFound = false, mpRoundEndFound = false;

            // Check events type
            if (items.Count() > 0)
            {
                ServiceCallItem first = items.First();
                if (first.m_host == "inGameEvents")
                {
                    ScanInGameEvnetRecords(ref mpRoundStartFound, ref mpRoundEndFound, items);
                }
                else if (first.m_host == "data-vef.xboxlive.com")
                {
                    ScanCs1EventRecords(ref mpRoundStartFound, ref mpRoundEndFound, items);
                }
                else if (first.m_host.EndsWith(".data.microsoft.com"))
                {
                    ScanCs2EventRecords(ref mpRoundStartFound, ref mpRoundEndFound, items);
                }
            }

            RuleResult result = InitializeResult(DisplayName, "");
            if (!mpRoundStartFound || !mpRoundEndFound)
            {
                result.Violations.Add(new Violation());
            }
            return result;
        }

        public override JObject SerializeJson()
        {
            return null;
        }

        private static void ScanInGameEvnetRecords(ref bool mpRoundStartFound, ref bool mpRoundEndFound, IEnumerable<ServiceCallItem> allCalls)
        {
            foreach (var call in allCalls)
            {
                if (call.m_eventName == "MultiplayerRoundStart")
                {
                    mpRoundStartFound = true;
                }
                else if (call.m_eventName == "MultiplayerRoundEnd")
                {
                    mpRoundEndFound = true;
                }

                if (mpRoundStartFound && mpRoundEndFound)
                {
                    return;
                }
            }
        }

        private void ScanCs1EventRecords(ref bool mpRoundStartFound, ref bool mpRoundEndFound, IEnumerable<ServiceCallItem> allCalls)
        {
            foreach (var call in allCalls)
            {
                var events = call.m_reqBody.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var e in events)
                {
                    var eventName = extractEventNameFromCS1Event(e);
                    if (eventName == "MultiplayerRoundStart")
                    {
                        mpRoundStartFound = true;
                    }
                    else if (eventName == "MultiplayerRoundEnd")
                    {
                        mpRoundEndFound = true;
                    }

                    if (mpRoundStartFound && mpRoundEndFound)
                    {
                        return;
                    }
                }
            }
        }

        private void ScanCs2EventRecords(ref bool mpRoundStartFound, ref bool mpRoundEndFound, IEnumerable<ServiceCallItem> allCalls)
        {
            foreach (var call in allCalls)
            {
                var events = call.m_reqBody.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var e in events)
                {
                    var eventName = extractEventNameFromCS2Event(e);
                    if (eventName == "MultiplayerRoundStart")
                    {
                        mpRoundStartFound = true;
                    }
                    else if (eventName == "MultiplayerRoundEnd")
                    {
                        mpRoundEndFound = true;
                    }

                    if (mpRoundStartFound && mpRoundEndFound)
                    {
                        return;
                    }
                }
            }
        }

        private string extractEventNameFromCS1Event(string eventBody)
        {
            // Example event :0.9.IGED-2.0|XDKS_01FFB41F.EnemyDefeated|....
            // We're extracting EnemyDefeated from that format
            var fields = eventBody.Split('|');
            if (fields.Count() > 2)
            {
                var subFields = fields[1].Split('.');
                // There should be at least 2 fields 
                if (subFields.Count() >= 2)
                {
                    return subFields[subFields.Count() - 1];
                }
            }

            return "";
        }

        private string extractEventNameFromCS2Event(string eventBody)
        {
            // Example event :{"ver":"2.1","name":"Microsoft.XboxLive.Txxxxxxxx.EnemyDefeated","time":"2015...
            // or {"ver":"2.1","name":"Microsoft.Xbox.XceBridge.CS.1.0.0.9.0.7.IGED-2.0.XDKS_0301D082.EnemyDefeated","time":"2016-...

            // We're extracting EnemyDefeated from that json format
            var eventJsonObjTemplate = new { name = "" };
            var eventJsonObj = JsonConvert.DeserializeAnonymousType(eventBody, eventJsonObjTemplate);

            var subFields = eventJsonObj.name.Split('.');
            // There should be at least 2 fields 
            if (subFields.Count() >= 2)
            {
                return subFields[subFields.Count() - 1];
            }

            return "";
        }
    }
}