using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#region Enums
public enum ExamMode
{
    Starting,
    Queued,
    Finished
}
#endregion

#region Answer
public class Answer
{
    public string Text { get; }
    public bool IsCorrect { get; }

    public Answer(string text, bool isCorrect)
    {
        Text = text;
        IsCorrect = isCorrect;
    }

    public override string ToString()
        => $"{Text} ({IsCorrect})";
}

public class AnswerList : List<Answer> { }
#endregion

#region Question Hierarchy
public abstract class Question : IComparable<Question>, ICloneable
{
    public string Header { get; set; }
    public string Body { get; set; }
    public double Marks { get; set; }
    public AnswerList Answers { get; set; }

    protected Question(string header, string body, double marks)
    {
        Header = header;
        Body = body;
        Marks = marks;
        Answers = new AnswerList();
    }

    public abstract void Display();

    public int CompareTo(Question other)
        => Marks.CompareTo(other.Marks);

    public object Clone()
        => MemberwiseClone();

    public override string ToString()
        => $"{Header} | {Body} | {Marks} Marks";

    public override bool Equals(object obj)
        => obj is Question q && Body == q.Body;

    public override int GetHashCode()
        => Body.GetHashCode();
}

public class TrueFalseQuestion : Question
{
    public TrueFalseQuestion(string h, string b, double m)
        : base(h, b, m) { }

    public override void Display()
    {
        Console.WriteLine($"{Body} (True / False)");
    }
}

public class ChooseOneQuestion : Question
{
    public ChooseOneQuestion(string h, string b, double m)
        : base(h, b, m) { }

    public override void Display()
    {
        Console.WriteLine(Body);

        foreach (var a in Answers)
        {
            Console.WriteLine($"- {a.Text}");
        }
    }
}

public class ChooseAllQuestion : Question
{
    public ChooseAllQuestion(string h, string b, double m)
        : base(h, b, m) { }

    public override void Display()
    {
        Console.WriteLine(Body);
         foreach(var a in Answers)
            Console.WriteLine($"[] {a.Text}");
    }
}
#endregion

#region QuestionList 
public class QuestionList<T> : List<T> where T : Question
{
    private readonly string filePath;

    public QuestionList(string path)
    {
        filePath = path;
    }

    public new void Add(T question)
    {
        base.Add(question);

        using StreamWriter sw = new StreamWriter(filePath, true);
        sw.WriteLine(question.ToString());
    }
}
#endregion

#region Subject
public class Subject
{
    public string Name { get; set; }

    public Subject(string name)
    {
        Name = name;
    }
}
#endregion

#region Exam Hierarchy
public abstract class Exam<T>
    where T : Question, ICloneable, IComparable<Question>
{
    public TimeSpan Time { get; set; }
    public Subject Subject { get; set; }
    public ExamMode Mode { get; protected set; }

    public QuestionList<T> Questions { get; set; }
    public Dictionary<T, AnswerList> QuestionAnswers { get; set; }

    public int NumberOfQuestions => Questions.Count;

    public event EventHandler ExamStarted;

    protected Exam(QuestionList<T> questions, Subject subject)
    {
        Questions = questions;
        Subject = subject;
        QuestionAnswers = new Dictionary<T, AnswerList>();
        Mode = ExamMode.Queued;
    }

    protected void OnExamStarted()
    {
        Mode = ExamMode.Starting;
        ExamStarted?.Invoke(this, EventArgs.Empty);
    }

    public abstract void ShowExam();
}

public class PracticeExam<T> : Exam<T>
    where T : Question, ICloneable, IComparable<Question>
{
    public PracticeExam(QuestionList<T> q, Subject s)
        : base(q, s) { }

    public override void ShowExam()
    {
        OnExamStarted();

        foreach (var q in Questions)
            q.Display();

        Console.WriteLine("\nCorrect Answers:");
        QuestionAnswers
            .Where(x => x.Value.Any(a => a.IsCorrect))
            .ToList()
            .ForEach(x =>
                Console.WriteLine(
                    $"{x.Key.Body} => " +
                    string.Join(",", x.Value.Where(a => a.IsCorrect))
                )
            );

        double totalMarks =
            QuestionAnswers
                .Where(x => x.Value.Any(a => a.IsCorrect))
                .Sum(x => x.Key.Marks);

        Console.WriteLine($"\nTotal Marks = {totalMarks}");

        Mode = ExamMode.Finished;
    }
}

public class FinalExam<T> : Exam<T>
    where T : Question, ICloneable, IComparable<Question>
{
    public FinalExam(QuestionList<T> q, Subject s)
        : base(q, s) { }

    public override void ShowExam()
    {
        OnExamStarted();

        Questions.ForEach(q => q.Display());

        Mode = ExamMode.Finished;
    }
}
#endregion

#region Student (Event Listener)
public class Student
{
    public string Name { get; set; }

    public Student(string name)
    {
        Name = name;
    }

    public void Notify(object sender, EventArgs e)
    {
        Console.WriteLine($"{Name} notified: Exam Started!");
    }
}
#endregion


class Program
{
    static void Main()
    {
        
        Subject oop = new Subject("OOP");

        var questions = new QuestionList<Question>("Questions.txt");
         //true or false 
        var q1 = new TrueFalseQuestion("Q1", "C# supports OOP?", 5);
        q1.Answers.Add(new Answer("True", true));
        q1.Answers.Add(new Answer("False", false));
        questions.Add(q1);

        // Choose One Question
        var q2 = new ChooseOneQuestion("Q2", "Which language is used for .NET?", 5);
        q2.Answers.Add(new Answer("Java", false));
        q2.Answers.Add(new Answer("C#", true));
        q2.Answers.Add(new Answer("Python",false));
        questions.Add(q2);

        // Choose All Question
        var q3 = new ChooseAllQuestion("Q3", "Select all OOP concepts", 10);
        q3.Answers.Add(new Answer("Encapsulation", true));
        q3.Answers.Add(new Answer("Polymorphism", true));
        q3.Answers.Add(new Answer("Inheritance", true));
        q3.Answers.Add(new Answer("Procedural", false));
        questions.Add(q3);
 
        //
        Console.WriteLine("Choose Exam Type: 1-Practice  2-Final");
        int choice = int.Parse(Console.ReadLine());

        Exam<Question> exam = choice == 1
            ? new PracticeExam<Question>(questions, oop)
            : new FinalExam<Question>(questions, oop);
 
        exam.QuestionAnswers.Add(q1, q1.Answers);
        exam.QuestionAnswers.Add(q2, q2.Answers);
        exam.QuestionAnswers.Add(q3, q3.Answers);

      
        var students = new List<Student>
        {
            new Student("Zeyad"),
            new Student("Ali")
            
        };

        // Event  
        foreach (var s in students)
        {
            exam.ExamStarted += s.Notify;
        }

        exam.ShowExam();
    }
}
