import java.time.*;
import java.time.zone.*;
import java.util.List;

public class ZoneInfoDisplay {
    public static void main(String[] args) {
        // タイムゾーンIDをコマンドライン引数から取得（デフォルトはAsia/Tokyo）
        String zoneIdString = (args.length > 0) ? args[0] : "Asia/Tokyo";
        ZoneId zoneId;
        try {
            zoneId = ZoneId.of(zoneIdString); // パラメータからZoneIdを生成
        } catch (DateTimeException e) {
            System.out.println("無効なタイムゾーンID: " + zoneIdString);
            return;
        }
        displayZoneInfo(zoneId); // 指定されたZoneIdで情報を表示
    }
    
public static void displayZoneInfo(ZoneId zoneId) {
        ZoneRules rules = zoneId.getRules();

        // 2. 過去の遷移リストを取得
        List<ZoneOffsetTransition> transitions = rules.getTransitions();
        // 3. 毎年繰り返される遷移ルールを取得（Asia/Tokyoでは通常空）
        List<ZoneOffsetTransitionRule> transitionRules = rules.getTransitionRules();

        // 4. 遷移が存在するかチェック
        if (transitions.isEmpty()) {
            // 遷移がない場合、現在の時差を表示
            ZoneOffset offset = rules.getOffset(Instant.now());
            System.out.println("遷移はありません。現在の時差: " + offset);
        } else {
            // 5. 最初の遷移前の時差を表示
            Instant firstInstant = transitions.get(0).getInstant().minusSeconds(1);
            ZoneOffset initialOffset = rules.getOffset(firstInstant);
            System.out.println("最初の遷移前の時差: " + initialOffset);

            // 6. 各遷移を表示
            for (ZoneOffsetTransition transition : transitions) {
                Instant instant = transition.getInstant();
                ZoneOffset offsetBefore = transition.getOffsetBefore();
                ZoneOffset offsetAfter = transition.getOffsetAfter();
                System.out.println("遷移時刻 " + instant + ": " + offsetBefore + " -> " + offsetAfter);
            }

            // 7. 最後の遷移後の時差を表示
            Instant lastInstant = transitions.get(transitions.size() - 1).getInstant().plusSeconds(1);
            ZoneOffset finalOffset = rules.getOffset(lastInstant);
            System.out.println("最後の遷移後の時差: " + finalOffset);
        }

        // 8. 遷移ルールが存在するかチェック
        if (!transitionRules.isEmpty()) {
            System.out.println("毎年繰り返される遷移ルール:");
            for (ZoneOffsetTransitionRule rule : transitionRules) {
                System.out.println(rule);
            }
        } else {
            System.out.println("毎年繰り返される遷移ルールはありません。");
        }
    }
}