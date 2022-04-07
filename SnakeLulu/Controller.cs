﻿using SnakeGameLibrary;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeLulu
{
    class Controller
    {
        private static Model model;
        private static View view;
        private static int currentLevelNumber;

        static void Main(string[] args)
        {
            model = Model.GetInstance();
            view = View.GetInstance(model);
            model.GameStatus = GameStatus.NewGame;

            SetOptions();

            ShowGameIntro();
            OpenMenu();
        }

        static void SetOptions()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.CursorVisible = false;
            Console.WindowWidth = 90;
            Console.WindowHeight = 30;
        }
        static void ShowGameIntro()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellation.Token;
            Task task = new Task(view.ShowGameIntro, cancellationToken);
            task.Start();
            Console.ReadKey();
            try
            {
                if (!task.IsCompleted)
                    cancellation.Cancel();
                task.Wait();
            }
            catch { }
        }

        static void OpenMenu()
        {
            bool isSelected = false;
            int index = 0;
            if (model.GameStatus == GameStatus.NewGame)
            {
                view.ShowMenu(MenuItems.StartGame);
                index = 0;
            }
            if (model.GameStatus == GameStatus.PausedGame)
            {
                view.ShowMenu(MenuItems.ContinueGame);
                index = 3;
            }

            while (!isSelected)
            {
                view.ShowMenu((MenuItems)index);
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        --index;
                        if (model.GameStatus == GameStatus.NewGame)
                        {
                            if (index == -1)
                                index = 2;
                        }
                        if (model.GameStatus == GameStatus.PausedGame)
                        {
                            if (index == -1)
                                index = 4;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        ++index;
                        if (model.GameStatus == GameStatus.NewGame)
                        {
                            if (index == 3)
                                index = 0;
                        }
                        if (model.GameStatus == GameStatus.PausedGame)
                        {
                            if (index == 5)
                                index = 0;
                        }
                        break;
                    case ConsoleKey.Enter:
                        isSelected = true;
                        break;
                }
            }

            Console.Clear();
            switch (index)
            {
                case 0:
                    currentLevelNumber = 1;
                    CreateNewGameLevel();
                    PlayGameAsync();
                    ControlGame();
                    break;
                case 1:
                    //TODO
                    break;
                case 2:
                    Environment.Exit(0);
                    break;
                case 3:
                    model.GameStatus = GameStatus.FlowGame;
                    PlayGameAsync();
                    ControlGame();
                    break;
                case 4:
                    //TODO
                    break;
            }
        }
        static void CreateNewGameLevel()
        {
            model.NewLevelInfo(currentLevelNumber);
            model.ClearModel();

            model.BuildWalls();

            model.BuildPlayer();
            
            model.BuildApples();

            model.GameStatus = GameStatus.FlowGame;
        }
        static async void PlayGameAsync()
        {
            await Task.Factory.StartNew(PlayGame);
        }
        static void PlayGame()
        {
            Console.Clear();
            view.DrawWalls();
            view.DrawPlayer();
            view.DrawApples();
            view.ShowLevelInfo();
            view.ShowCountdownBefore();//HACK canceletion token
            view.ShowLevelScore();

            while (model.GameStatus == GameStatus.FlowGame)
            {
                model.MovePlayer();
                view.DrawPlayer();

                if (model.CheckCoordinateApplesForPlayer())
                {
                    if(model.CheckForChangesAfterAddingScore())
                    {
                        view.PutAwayApples();
                        view.DrawGate();
                    }
                    else
                    {
                        view.DrawApples();
                    }
                    view.ShowLevelScore();
                }
                
                if (model.CheckCoordinateWalls())
                {
                    model.GameStatus = GameStatus.OverGame;
                }
                if (model.CheckCoordinateGate())
                {
                    currentLevelNumber++;
                    //TODO smooth transition
                    model.GameStatus = GameStatus.NextLevel;
                }

                if (model.CheckCoordinatePlayerForPlayer())
                {
                    model.GameStatus = GameStatus.OverGame;
                }
                Thread.Sleep(300 - (model.Player.Speed * 20));
            }

            if (model.GameStatus == GameStatus.OverGame)
            {
                view.ShowEndGame();
            }
            if (model.GameStatus == GameStatus.NextLevel)
            {
                view.ShowMessageNextLevel();
            }
        }
        static void ControlGame()
        {
            while (model.GameStatus == GameStatus.FlowGame)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.UpArrow:
                        model.Player.Turn(Direction.Forward);
                        break;
                    case ConsoleKey.DownArrow:
                        model.Player.Turn(Direction.Back);
                        break;
                    case ConsoleKey.LeftArrow:
                        model.Player.Turn(Direction.Left);
                        break;
                    case ConsoleKey.RightArrow:
                        model.Player.Turn(Direction.Right);
                        break;
                    case ConsoleKey.Escape:
                        model.GameStatus = GameStatus.PausedGame;
                        OpenMenu();
                        break;
                }
                Thread.Sleep(300 - (model.Player.Speed * 20));//TODO Задержка перед выходом в меню
            }
            if (model.GameStatus == GameStatus.OverGame)
            {
                model.GameStatus = GameStatus.NewGame;
                OpenMenu();
            }
            if (model.GameStatus == GameStatus.NextLevel)
            {
                CreateNewGameLevel();
                PlayGameAsync();
                ControlGame();
            }
        }
    }
}
