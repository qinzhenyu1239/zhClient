using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TimerMgr : UnitySingleton<TimerMgr>
{
    public delegate void TimerHandler(object param);
    class TimerNode {
        public int timerID;
        public TimerHandler OnTimer;
        public object param;
        public int repeat;
        public float nextTriggerTime;
        public float curTime;
        public float interval;
        public bool isCancel;
    }

    private Dictionary<int, TimerNode> timerNodes;
    private int autoTimerID = 1;
    private List<int> removeTimerQueue;

    public void Init() {
        this.timerNodes = new Dictionary<int, TimerNode>();
        this.removeTimerQueue = new List<int>();
        this.autoTimerID = 1;
    }

    void Update()
    {
        if (this.timerNodes == null) {
            return;
        }

        foreach (var key in this.timerNodes.Keys) {
            TimerNode timerNode = this.timerNodes[key];
            if (timerNode == null || timerNode.isCancel == true) {
                continue;
            }

            timerNode.curTime += Time.deltaTime;
            if (timerNode.nextTriggerTime <= timerNode.curTime) {
                timerNode.OnTimer(timerNode.param);

                timerNode.nextTriggerTime = timerNode.interval;
                timerNode.curTime = 0;

                if (timerNode.repeat != -1) {
                    timerNode.repeat--;
                }

                if (timerNode.repeat == 0) {  // 定时器执行结束了, 就要删除掉;
                    this.UnShedule(timerNode.timerID);
                }
            }
        }

        // 清理掉过期Timer
        /*foreach (var key in this.timerNodes.Keys) {
            TimerNode timerNode = this.timerNodes[key];
            if (timerNode == null || timerNode.isCancel == false) {
                continue;
            }
            // this.timerNodes.Remove(key); // 删除待定
            this.removeTimerQueue.Add(key);
        }*/
        
        for (int i = 0; i < this.removeTimerQueue.Count; i++) {
            if (this.timerNodes.ContainsKey(this.removeTimerQueue[i])) {
                this.timerNodes.Remove(this.removeTimerQueue[i]);
            }
        }
        this.removeTimerQueue.Clear();
    }

    public int Schedule(TimerHandler OnTimer, object param, 
                        int repeat, float interval, float delay) {
        
        if (OnTimer == null || interval < 0.0f || delay < 0.0f) {
            return 0;
        }

        int timerID = this.autoTimerID ++;
        this.autoTimerID = (this.autoTimerID == 0) ? 1 : this.autoTimerID;

        TimerNode timerNode = new TimerNode();
        timerNode.OnTimer = OnTimer;
        timerNode.param = param;
        timerNode.repeat = (repeat <= 0)? -1 : repeat;
        timerNode.interval = interval;
        timerNode.curTime = 0;
        timerNode.nextTriggerTime = delay;
        timerNode.timerID = timerID;
        timerNode.isCancel = false;

        this.timerNodes.Add(timerID, timerNode);

        return timerID;
    }

    public void UnShedule(int timerID) {
        if (this.timerNodes.ContainsKey(timerID)) {
            TimerNode timerNode = this.timerNodes[timerID];
            if (timerNode != null) {
                timerNode.isCancel = true;
                this.removeTimerQueue.Add(timerID);
            }
        }
    }

    public int Schedule(TimerHandler OnTimer, int repeat, 
                        float interval, float delay)
    {
        return Schedule(OnTimer, null, repeat, interval, delay);
    }

    public int ScheduleOnce(TimerHandler OnTimer, object param, float delay) {
        return Schedule(OnTimer, param, 1, 0, delay);
    }

    public int ScheduleOnce(TimerHandler OnTimer, float delay) {
        return Schedule(OnTimer, null, 1, 0, delay);
    }
}
