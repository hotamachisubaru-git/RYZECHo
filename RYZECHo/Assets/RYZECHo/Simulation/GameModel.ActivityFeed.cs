namespace RYZECHo;

public sealed partial class GameModel
{
    private void PushActivityFeed(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_activityFeed.Count == 0 || _activityFeed[0] != message)
        {
            _activityFeed.Insert(0, message);
        }

        if (_activityFeed.Count > 5)
        {
            _activityFeed.RemoveRange(5, _activityFeed.Count - 5);
        }
    }

    private void SetResultMessage(string message)
    {
        _resultMessage = message;
        PushActivityFeed(message);
    }
}
