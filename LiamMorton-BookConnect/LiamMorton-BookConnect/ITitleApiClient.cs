public interface ITitleApiClient
{
    //send and recive request from client
    Task<TitleDto> GetTitleAsync(string isbn);
    //parse json and format to TitleDto requerments
    TitleDto ParseJson(string json);
}
