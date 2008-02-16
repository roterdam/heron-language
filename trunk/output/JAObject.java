package jaction;

public class JAObject {
	void send(Object o) {
		sendIn(o, 0);
	}
	void sendIn(Object o, int msec) {
		JASignalDispatcher.sendIn(this, o, msec);
	}
	void dispatch(JASignal sig) {
		// Do nothing by default
		// active objects should overload this function 	
	}
}
