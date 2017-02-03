﻿using System;

namespace seng403alarmclock {
    /// <summary>
    /// Internal representation of an alarm, add to it as necessairy
    /// 
    /// alarmTime is the the time the alarm will go off at
    /// 
    /// alarmName is a unique name for the alarm (the time it was created) so that we can id alarms later and remove them
    /// </summary>
    public class Alarm {

        private DateTime alarmTime { get; set; }
        private String alarmName { get; set; }

        /// <summary>
        /// Create an alarm 5 minutes from now
        /// </summary>
        public Alarm()
        {
            alarmTime = DateTime.Now.AddMinutes(5);
            alarmName = "Created:" + DateTime.Now.ToString();
        }

        public Alarm(DateTime alarmTime) {
            this.alarmTime = alarmTime;
            alarmName = "Created:" + DateTime.Now.ToString();

        }

        public DateTime GetAlarmTime() {
            //TODO: return a reasonable time
            return alarmTime;
        }

        
    }
}