using EchoesOfTheShardveil.Desktop;

if (args is ["--alpha-duel"])
{
    AlphaDuelConsole.Run();
    return;
}

using var game = new CardGame();
game.Run();
