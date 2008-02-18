
import java.util.ArrayList;

public class HeronDispatcher extends Thread {
	public static ArrayList<HeronSignal> signals = new ArrayList<HeronSignal>();

	public static void sendIn(HeronObject target, Object data, int msec) {
		sendAt(target, data, (new HeronDateTime()).addMSec(msec));
	}
	public static void sendAt(HeronObject target, Object data, HeronDateTime time) {
		HeronSignal sig = new HeronSignal();
		sig.data = data;
		sig.scheduledTime = time;
		sig.target = target;
		signals.add(sig);
	}
	public static HeronSignal getNextSignal()
	{
		// Improvement: use a priority queue
		HeronSignal ret = null;
		HeronDateTime now = new HeronDateTime();
		for (int i=0; i < signals.size(); ++i) {
			if (ret == null)
				ret = signals.get(i);
			else {
				HeronSignal tmp = signals.get(i);
				if (tmp.scheduledTime.before(ret.scheduledTime) 
					&& tmp.scheduledTime.before(now)) 
					ret = signals.get(i);
			}
		}
		if (ret != null)
			signals.remove(ret);
		return ret;
	}
	public static void dispatchNextSignal() {
		HeronSignal sig = getNextSignal();
		if (sig != null) {
			sig.target.dispatch(sig);
		}
	}
}

