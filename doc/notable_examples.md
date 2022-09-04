# Notable Examples

Harmony can be a bit tricky to learn the first time around. This file contains examples for some of the more annoying kinds of harmony injections.

## [Simplest Example](https://github.com/toasterparty/oc2-modding/blob/58efd3ab97668fd8047bf3ad79c129374bfb624a/OC2Modding/CustomOrderLifetime.cs#L26..L31)

If the circumstances are correct, this is the ideal way to patch a method.

## [Patch Subclass + Private Method](https://github.com/toasterparty/oc2-modding/blob/55dc329ebfd0d89b3352235c85f24e8daf855512/OC2Modding/DisplayModsOnResultsScreen.cs#L144..L149)

You can patch subclasses like so. You can also patch private methods by just putting the method name in quotes.

Sometimes the class you want to patch requires an additional `using` at the top of the file (gold text in DnSpy).

## [Multiple Methods w/ One Patch](https://github.com/toasterparty/oc2-modding/blob/55dc329ebfd0d89b3352235c85f24e8daf855512/idea/DisplayLeaderboardScores.cs#L239..L254)

Sometimes it's beneficial to bulk-patch multiple methods. You can do that by using `AccessTools`.

## [Read/Write Private Class Data](https://github.com/toasterparty/oc2-modding/blob/9c748c8453682b6b2e120802755ca7a9705587eb/OC2Modding/LevelProgression.cs#L106..L119)

Normally when patching, you only have access to public class data. Reflecting can be abused to circumvent this.

## [Patch Method With Overloads](https://github.com/toasterparty/oc2-modding/blob/7d284ff5db821f27cd5ee3e0868e744629405539/OC2Modding/Nerfs.cs#L269)

You can specify the argument types to hint at which method you want patched when multiple share the same name.

## [Invoke Private Method](https://github.com/toasterparty/oc2-modding/blob/9eb4eab953be71ab5da4f2e17ecda37a136c26af/OC2Modding/ArchipelagoClient/ArchipelagoLoginGUI.cs#L36..L37)

Since we are patching classes from outside their original scope, private/protected methods which are designed to be useful within a class are not accessible by default. Some reflection handwaving can be used to get around this.
