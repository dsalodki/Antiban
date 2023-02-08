using System;
using System.Collections.Generic;
using System.Linq;

namespace Antiban
{
    public class Antiban
    {
        private List<EventMessage> _eventMessages = new List<EventMessage>();
        private bool _yetOneCycle = false;

        /// <summary>
        /// Добавление сообщений в систему, для обработки порядка сообщений
        /// </summary>
        /// <param name="eventMessage"></param>
        public void PushEventMessage(EventMessage eventMessage)
        {
            //TODO
            _eventMessages.Add(eventMessage);
        }

        /// <summary>
        /// Вовзращает порядок отправок сообщений
        /// </summary>
        /// <returns></returns>
        public List<AntibanResult> GetResult()
        {
            //TODO
            var result = new List<AntibanResult>();

            var sortedEM = Clone(_eventMessages.OrderBy(x => x.DateTime).ToList());
            
            var sendTime = new DateTime();
            var time = new DateTime();
            var time1 = new DateTime();
            EventMessage prev = null;
            EventMessage prevSamePhone = null;
            int prevId;
            foreach (var item in sortedEM)
            {
                cycle:
                prevId = result.OrderBy(x => x.SentDateTime).LastOrDefault(x => x.SentDateTime <= item.DateTime)?.EventMessageId ?? 0;
                if (prevId > 0)
                {
                    prev = sortedEM.First(x => x.Id == prevId);
                }

                if (prevId == 0)
                {
                    result.Add(new AntibanResult
                    {
                        EventMessageId = item.Id,
                        SentDateTime = item.DateTime
                    });
                }
                else
                if (item.Priority != 1)
                {
                    var phoneSet = sortedEM.Where(x=>x.Phone == item.Phone && x.DateTime <= item.DateTime).Select(x=>x.Id).ToList();
                    int? prevSamePhoneId = result.OrderBy(x=>x.SentDateTime).LastOrDefault(x => phoneSet.Contains(x.EventMessageId) && x.SentDateTime <= item.DateTime)?.EventMessageId;
                    if(prevSamePhoneId == null)
                    {
                        prevSamePhone = null;
                    }
                    else
                    {
                        prevSamePhone = sortedEM.First(x => x.Id == prevSamePhoneId);
                    }
                    calculateNextSending(prev, prevSamePhone, item, result);
                }
                else
                {
                    var set = sortedEM.Where(x=>x.Priority == 1 && x.DateTime <= item.DateTime && x.Phone== item.Phone).Select(x=>x.Id).ToList();

                    prevId = result.OrderBy(x => x.SentDateTime).LastOrDefault(x=>set.Contains(x.EventMessageId))?.EventMessageId ?? 0;
                    if (prevId == 0)
                    {
                        prevId = result.OrderBy(x => x.SentDateTime).LastOrDefault(x => x.SentDateTime <= item.DateTime).EventMessageId;
                        prev = sortedEM.First(x => x.Id == prevId);

                        var phoneSet = sortedEM.Where(x => x.Phone == item.Phone && x.DateTime <= item.DateTime).Select(x => x.Id).ToList();
                        int? prevSamePhoneId = result.OrderBy(x => x.SentDateTime).LastOrDefault(x => phoneSet.Contains(x.EventMessageId) && x.SentDateTime <= item.DateTime)?.EventMessageId;
                        if (prevSamePhoneId == null)
                        {
                            prevSamePhone = null;
                        }
                        else
                        {
                            prevSamePhone = sortedEM.First(x => x.Id == prevSamePhoneId);
                        }
                        calculateNextSending(prev, prevSamePhone, item, result);
                    }
                    else
                    {
                        time = result.First(x => x.EventMessageId == prevId).SentDateTime.AddHours(24);

                        prev = sortedEM.First(x => x.Id == prevId);

                        if (prev.Phone == item.Phone)
                        {
                            time = time > result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddMinutes(1) ? time : result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddMinutes(1);
                            sendTime = time;
                        }
                        else
                        {
                            time = time > result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddSeconds(10) ? time : result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddSeconds(10);
                            sendTime = time;


                            var phoneSet = sortedEM.Where(x => x.Phone == item.Phone && x.DateTime <= item.DateTime).Select(x => x.Id).ToList();
                            int? prevSamePhoneId = result.OrderBy(x => x.SentDateTime).LastOrDefault(x => phoneSet.Contains(x.EventMessageId) && x.SentDateTime <= item.DateTime)?.EventMessageId;
                            if (prevSamePhoneId == null)
                            {
                                prevSamePhone = null;
                            }
                            else
                            {
                                prevSamePhone = sortedEM.First(x => x.Id == prevSamePhoneId);
                            }

                            if (prevSamePhone != null)
                            {
                                time1 = result.First(x => x.EventMessageId == prevSamePhone.Id).SentDateTime.AddMinutes(1);

                                sendTime = sendTime > time1 ? sendTime : time1;
                            }
                        }

                        if (sendTime < item.DateTime)
                        {
                            sendTime = item.DateTime;
                        }

                        if (result.Select(x => x.SentDateTime).Contains(sendTime))
                        {
                            item.DateTime = sendTime;
                            _yetOneCycle = true;
                        }
                        else
                        {
                            _yetOneCycle = false;
                            result.Add(new AntibanResult { EventMessageId = item.Id, SentDateTime = sendTime });
                        }
                    }
                }

                if(_yetOneCycle)
                {
                    goto cycle;
                }
            }

            return result.OrderBy(x => x.SentDateTime).ToList();
        }

        private List<EventMessage> Clone(List<EventMessage> oldList)
        {
            List<EventMessage> newList = new List<EventMessage>(oldList.Count);

            oldList.ForEach((item) =>
            {
                newList.Add(new EventMessage(item.Id, item.Phone, item.DateTime, item.Priority));
            });

            return newList;
        }

        private void calculateNextSending(EventMessage prev, EventMessage prevSamePhone, EventMessage item, List<AntibanResult> result)
        {
            DateTime time;
            DateTime time1;
            DateTime sendTime = new DateTime();
            if (prev.Phone == item.Phone)
            {
                time = result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddMinutes(1);

                sendTime = time;
            }
            else
            {
                time = result.First(x => x.EventMessageId == prev.Id).SentDateTime.AddSeconds(10);

                sendTime = time;

                if (prevSamePhone != null)
                {
                    var prevSamePhoneResult = result.FirstOrDefault(x => x.EventMessageId == prevSamePhone.Id);
                    if(prevSamePhoneResult != null)
                    {
                        time1 = prevSamePhoneResult.SentDateTime.AddMinutes(1);

                        sendTime = time1;
                    }
                }
            }

            if (sendTime < item.DateTime)
            {
                sendTime = item.DateTime;
            }

            if (result.Select(x => x.SentDateTime).Contains(sendTime))
            {
                item.DateTime= sendTime;
                _yetOneCycle = true;
                return;
            }

            //if (sendTime < item.ExpireDateTime)
            {
                _yetOneCycle = false;
                result.Add(new AntibanResult { EventMessageId = item.Id, SentDateTime = sendTime });
            }
        }
    }
}
