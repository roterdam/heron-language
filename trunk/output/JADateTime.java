package jaction;

import java.util.Calendar;
import java.util.GregorianCalendar;

public class JADateTime  {
	public Calendar calendar;
	public JADateTime() {
		calendar = GregorianCalendar.getInstance();
	}
	public JADateTime(Calendar cal) {
		calendar = cal;
	}
	public int getMSec() {
		return calendar.get(Calendar.MILLISECOND);
	}
	public int getMSecElapsedUntil(JADateTime time) {
		return getMSec() - time.getMSec();
	}
	public int getMSecElapsedSince() {
		return getMSecElapsedUntil(new JADateTime());
	}
	public JADateTime addMSec(int n) {
		Calendar cal = (Calendar)calendar.clone();
		cal.add(Calendar.MILLISECOND, n);
		return new JADateTime(cal);
	}
	public boolean before(JADateTime time) { 
		return getMSecElapsedUntil(time) > 0;
	}
	public boolean after(JADateTime time) {
		return getMSecElapsedUntil(time) < 0;
	}
	public boolean close(JADateTime time) {
		return getMSecElapsedUntil(time) == 0;
	}
}