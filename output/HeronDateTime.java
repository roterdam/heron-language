import java.util.Calendar;
import java.util.GregorianCalendar;

public class HeronDateTime  {
	public Calendar calendar;
	public HeronDateTime() {
		calendar = GregorianCalendar.getInstance();
	}
	public HeronDateTime(Calendar cal) {
		calendar = cal;
	}
	public int getMSec() {
		return calendar.get(Calendar.MILLISECOND);
	}
	public int getMSecElapsedUntil(HeronDateTime time) {
		return getMSec() - time.getMSec();
	}
	public int getMSecElapsedSince() {
		return getMSecElapsedUntil(new HeronDateTime());
	}
	public HeronDateTime addMSec(int n) {
		Calendar cal = (Calendar)calendar.clone();
		cal.add(Calendar.MILLISECOND, n);
		return new HeronDateTime(cal);
	}
	public boolean before(HeronDateTime time) { 
		return getMSecElapsedUntil(time) > 0;
	}
	public boolean after(HeronDateTime time) {
		return getMSecElapsedUntil(time) < 0;
	}
	public boolean close(HeronDateTime time) {
		return getMSecElapsedUntil(time) == 0;
	}
}