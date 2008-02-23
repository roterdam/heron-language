import java.util.Calendar;
import java.util.GregorianCalendar;

public class HeronDateTime  {
	public Calendar calendar;
	public HeronDateTime() {
		calendar = GregorianCalendar.getInstance();
	}
	public HeronDateTime(Calendar cal) {
		calendar = (Calendar)cal.clone();
	}
	public long getMSec() {
		return calendar.getTimeInMillis();
	}
	public long getMSecElapsedUntil(HeronDateTime time) {
		return getMSec() - time.getMSec();
	}
	public long getMSecElapsedSince() {
		return getMSecElapsedUntil(new HeronDateTime());
	}
	public HeronDateTime addMSec(int n) {
		Calendar cal = (Calendar)calendar.clone();
		cal.add(Calendar.MILLISECOND, n);
		return new HeronDateTime(cal);
	}
	public boolean before(HeronDateTime time) { 
		return calendar.before(time.calendar);
	}
	public boolean after(HeronDateTime time) {
		return time.calendar.before(calendar);
	}
}