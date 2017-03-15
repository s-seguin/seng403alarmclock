﻿using seng403alarmclock.Data;
using seng403alarmclock.GUI;
using System;
using System.Collections.Generic;

namespace seng403alarmclock.Model
{
    class AlarmController : GUI.GuiEventListener, TimeListener
    {
        #region fields and Properties

        //locals
        private List<Alarm> alarmList;
        private AudioController audioController;
        private GuiController guiController;

        private DateTime snoozeUntilTime;

        //statics
        private static int snoozePeriod_minutes = 1;
        private static int maxSnoozePeriod_minutes = 40;

        //Properties
        public DateTime SnoozeUntilTime
        {
            get
            {
                return this.snoozeUntilTime;
            }
        }
        #endregion

        #region constructors

        /// <summary>
        /// Constructor: initializes snoozeUntilTime to harmless value
        /// </summary>
        public AlarmController()
        {
            this.alarmList = new List<Alarm>();
            this.audioController = AudioController.GetController();
            this.guiController = GuiController.GetController();
            this.snoozeUntilTime = TimeFetcher.getCurrentTime();
        }

        #endregion

        #region dismiss/cancel alarms

        public void AlarmCanceled(Alarm alarm, bool wasPreEmpted)
        {
            alarmList.Remove(alarm);
            guiController.RemoveAlarm(alarm, wasPreEmpted);
        }

        //dismiss refers to turning off a ringing/snoozing alarm
        public void AlarmDismissed(bool dueToPreEmpt)
        {
            for (int i = alarmList.Count - 1; i >= 0; i--)
            {
                Alarm a = alarmList[i];
                if (a.IsRinging || a.IsSnoozing)
                {
                    a.Status = AlarmState.Off;
                    try
                    {
                        //try to move the alarm to its next scheduled time
                        //catch the exception if this was the last time, then remove it
                        a.CalculateNextAlarmTime();
                        //graphically update the GUI since the alarm state has changed
                        guiController.UpdateAlarm(a);
                    }
                    catch (NoMoreAlarmsException)
                    {
                        AlarmCanceled(a, dueToPreEmpt);
                    }
                    guiController.Dismiss_Btn_setHidden();
                    guiController.Snooze_Btn_setHidden();
                }
            }
           
        }

        #endregion

        #region Check / Trigger Alarm

        public void TimePulse(DateTime currentTime)
        {
            this.CheckAlarms(currentTime);
        }

        /// <summary>
        /// cycles through list of alarms to see which is ready to go off, then calls TriggerAlarm on each of them
        /// </summary>
        private void CheckAlarms(DateTime now)
        {
            if (CheckIfSnoozeOver(now))
                for (int i = alarmList.Count - 1; i >= 0; i--)
                    if (CheckIfAlarmIsDue(alarmList[i], now) && (!alarmList[i].IsRinging))
                        TriggerAlarm(alarmList[i]);                    
                    else
                        UpdateGUI_SnoozeRemaining_minutes(now);
                
        }

        /// <summary>
        /// requests that the audio controller begin ringing with the ringtone associated with the alarm
        /// </summary>
        /// <param name="alarm"></param>
        private void TriggerAlarm(Alarm alarm)
        {

            AlarmDismissed(true); //new alarms preempt old ones
            alarm.Status = AlarmState.Ringing;
            guiController.UpdateAlarm(alarm);

            guiController.Snooze_Btn_setVisible();
            guiController.DismissAll_Btn_setVisible();
        }

        #endregion

        #region Alarm Requests

        public void AlarmRequested(int hour, int minute, bool repeat, string audioFile, bool weekly, List<DayOfWeek> days, string AlarmName)
        {
            Alarm newAlarm = new Alarm(hour, minute, repeat, audioFile, weekly, days, AlarmName);
            GuiController.GetController().AddAlarm(newAlarm);
            alarmList.Add(newAlarm);
        }

        #endregion

        #region snooze

        /// <summary>
        /// snooze alarms from being able to ring for snoozePeriod_minutes
        /// </summary>
        public void SnoozeRequested()
        {
            DateTime now = TimeFetcher.getCurrentTime();
            if (CheckIfSnoozeOver(now))
            {
                updateSnoozeUntilTime(now);
                guiController.Snooze_Btn_setHidden();                
            }
        }

        /// <summary>
        /// sets snoozePeriod_minutes. if period > maxSnoozePeriod_minutes, sets it to maxSnoozePeriod_minutes instead. 
        /// </summary>
        public void SnoozePeriodChangeRequested(int minutes)
        {
            if (minutes < maxSnoozePeriod_minutes)
                AlarmController.snoozePeriod_minutes = minutes;
            else
                AlarmController.snoozePeriod_minutes = maxSnoozePeriod_minutes;
        }

        #endregion

        #region helper functions
        //these are mostly to make the above functions more readable

        private bool CheckIfAlarmIsDue(Alarm alarm, DateTime now)
        {
            return (alarm.GetAlarmTime().CompareTo(now) <= 0);
        }

        private void updateSnoozeUntilTime(DateTime now)
        {
            //changed this so it doesn't allways go off at an exact minute, its weird if you hit snooze at 11:59:59 and the snooze ends immedietly - Patrick
            this.snoozeUntilTime = now.AddMinutes(AlarmController.snoozePeriod_minutes);
        }

        private bool CheckIfSnoozeOver(DateTime currentTime)
        {
            return (this.snoozeUntilTime.CompareTo(currentTime) <= 0);
        }

        private void UpdateGUI_SnoozeRemaining_minutes(DateTime now)
        {
            //This does a different thing than you think it does - Patrick
            //this.guiController.SetSnoozeDisplayTime(this.snoozeUntilTime.Subtract(now).Minutes);
        }

        #endregion

        #region ManualTimeChange

        public void ManualTimeRequested(int hours, int minutes)
        {
            TimeFetcher.SetNewTime(hours, minutes);
        }

        public void ManualDateRequested(int year, int month, int day)
        {
            TimeFetcher.SetNewDate(year, month, day);
        }

        #endregion

        /// <summary>
        /// REQUIRE:
        ///     main Window EXISTS
        /// </summary>
        public void SetupMainWindow()
        {
            //attempt to load the alarm list from data and push them onto the GUI
            try
            {
                alarmList = (List<Alarm>)DataDriver.Instance.GetVariable("AlarmList");
                foreach (Alarm alarm in alarmList)
                {
                    guiController.AddAlarm(alarm);
                }
            }
            catch (IndexOutOfRangeException)
            {
                //variable didn't exist in the array, the default one is empty and will work
            }

        }

        public void Teardown()
        {
            //before we save, lets turn all the alarm ringing off
            foreach (Alarm alarm in alarmList)
            {
                alarm.Status = AlarmState.Off;
            }
            //save the alarm list to the data driver
            DataDriver.Instance.SetVariable("AlarmList", alarmList);
            DataDriver.Instance.shutdown(); //rewrite the save file, to handle unexpected shutdown
        }

        public void AlarmEdited(Alarm alarm, int hour, int minute, bool repeat, string audioFile, bool weekly, List<DayOfWeek> days)
        {
            alarm.EditAlarm(hour, minute, repeat, audioFile, weekly, days);
            //reflect the changes in the gui
            GuiController.GetController().EditAlarm(alarm, alarmList);


        }

        /// <summary>
        /// Called when the main window is shutting down
        /// </summary>
        public void MainWindowShutdown()
        {
            Teardown();
        }

        public void TimeZoneOffsetChanged(double offset) {}
    }
}



