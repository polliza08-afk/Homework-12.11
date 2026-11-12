using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfTest.Entities;

namespace WpfTest;

public partial class MainWindow : Window
{
    private readonly string _testTitle;
    private readonly List<QuestionEntity> _questions;
    private int _index; //Номер поточного питання
    private int _correct; //Кількість правильних відповідей
    private int _wrong; //Кількість неправильних відповідей

    public MainWindow()
    {
        InitializeComponent();

        //Завантажуємо перший тест з БД разом із питаннями та відповідями
        using var db = new MyDataContext();
        var test = db.Tests
            .Include(t => t.Questions)
            .ThenInclude(q => q.Answers)
            .First();

        _testTitle = test.Title;
        _questions = test.Questions.OrderBy(q => q.Id).ToList();
        Title = _testTitle;

        ShowQuestion();
    }

    //Відображає поточне питання та генерує кнопки для варіантів відповідей
    private void ShowQuestion()
    {
        var question = _questions[_index];

        ProgressBar.Maximum = _questions.Count;
        ProgressBar.Value = _index;
        ProgressText.Text = $"Питання {_index + 1} з {_questions.Count}";
        QuestionText.Text = question.Text;

        AnswersPanel.Children.Clear();

        //Якщо правильна відповідь одна — ставимо RadioButton,
        //якщо кілька — CheckBox
        bool singleChoice = question.Answers.Count(a => a.IsCorrect) == 1;
        foreach (var answer in question.Answers.OrderBy(a => a.Id))
        {
            ToggleButton option = singleChoice
                ? new RadioButton()
                : new CheckBox();
            option.Content = answer.Text;
            option.Tag = answer; // Прив'язуємо об'єкт відповіді до кнопки
            option.Margin = new Thickness(0, 4, 0, 4);
            AnswersPanel.Children.Add(option);
        }

        NextButton.Content = _index == _questions.Count - 1 ? "Завершити" : "Далі";
    }

    //Перевіряє відповідь при натисканні кнопки "Далі"
    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        var question = _questions[_index];

        //Отримуємо всі позначені користувачем варіанти
        var selected = AnswersPanel.Children.OfType<ToggleButton>()
            .Where(c => c.IsChecked == true)
            .Select(c => (AnswerEntity)c.Tag)
            .ToList();
        var correctCount = question.Answers.Count(a => a.IsCorrect);

        //Зараховуємо бал, лише якщо вибрані всі правильні варіанти і жодного зайвого
        bool isCorrect = selected.Count > 0
            && selected.All(a => a.IsCorrect)
            && selected.Count == correctCount;

        if (isCorrect) _correct++;
        else _wrong++;

        _index++;
        if (_index < _questions.Count)
        {
            ShowQuestion();
            return;
        }

        ShowResult();
    }

    //Показує фінальне вікно з результатами
    private void ShowResult()
    {
        ProgressBar.Value = _questions.Count;
        ProgressText.Text = $"Пройдено {_questions.Count} з {_questions.Count}";
        QuestionText.Text = string.Empty;
        AnswersPanel.Children.Clear();
        NextButton.IsEnabled = false;

        MessageBox.Show(
            $"Тест: {_testTitle}\n" +
            $"Правильних відповідей: {_correct}\n" +
            $"Неправильних відповідей: {_wrong}\n" +
            $"Всього питань: {_questions.Count}",
            "Результат");
    }
}