namespace NeoTwitch.Services.Text;

public interface IUiTextService
{
    string Get(string key);

    string Format(string key, params object[] args);
}
