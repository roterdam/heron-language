package jaction;

import java.util.ArrayList;

public class JASignalDispatcher {
	public static void sendIn(JAObject target, Object data, int msec) {
		sendAt(target, data, (new JADateTime()).addMSec(msec));
	}
	public static void sendAt(JAObject target, Object data, JADateTime time) {
		JASignal sig = new JASignal();
		sig.data = data;
		sig.scheduledTime = time;
		sig.target = target;
		signals.add(sig);
	}
	public static JASignal getNextSignal()
	{
		// Improvement: use a priority queue 
		JASignal ret = null;
		JADateTime now = new JADateTime();
		for (int i=0; i < signals.size(); ++i) {
			if (ret == null)
				ret = signals.get(i);
			else {
				JASignal tmp = signals.get(i);
				if (tmp.scheduledTime.before(ret.scheduledTime) 
					&& tmp.scheduledTime.before(now)) 
					ret = signals.get(i);
			}
		}
		if (ret != null)
			signals.remove(ret);
		return ret;
	}
	public static void dispatchSignal() {
		Object o = new Object();
		while (true) {
			JASignal sig = getNextSignal();
			if (sig != null) {
				sig.target.dispatch(sig); 
			}
			// Improvement: wait the precise amount of time needed
			try {
				o.wait(10);
			}
			catch (Exception e) {			
			}
		}
	}
	public static ArrayList<JASignal> signals = new ArrayList<JASignal>();
}

