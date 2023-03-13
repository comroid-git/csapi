using System.Numerics;
using SFML.System;

namespace comroid.gamelib;

public static class Extensions
{
    public static Vector2 To2(this Vector3 it) => new(it.X, it.Y);
    public static Vector2 To2(this Vector2f it) => new(it.X, it.Y);
    public static Vector3 To3(this Vector2 it, float z = 0) => new(it.X, it.Y, z);
    public static Vector3 To3(this Vector2f it, float z = 0) => new(it.X, it.Y, z);
    public static Vector3 To3(this Vector2i it, float z = 0) => new(it.X, it.Y, z);
    public static Vector3 To3(this Vector2u it, float z = 0) => new(it.X, it.Y, z);
    public static Vector2f To2f(this Vector2 it) => new(it.X, it.Y);
    public static Vector2f To2f(this Vector3 it) => new(it.X, it.Y);
    
    // this method brought to you by ChatGPT
    public static Vector3 Euler(this Quaternion q)
    {
        // Store the Euler angles in radians
        float heading, attitude, bank;

        // Determine the rotation order based on the largest absolute component of the Quaternion
        var sqw = q.W * q.W;
        var sqx = q.X * q.X;
        var sqy = q.Y * q.Y;
        var sqz = q.Z * q.Z;
        var unit = sqx + sqy + sqz + sqw; // Normalised quaternion will have sqw = 1

        // Singularity at north pole
        if (q.X * q.Y + q.Z * q.W == 0.5 * unit)
        {
            heading = 2 * (float)Math.Atan2(q.X, q.W);
            attitude = (float)Math.PI / 2;
            bank = 0;
        }
        // Singularity at south pole
        else if (q.X * q.Y + q.Z * q.W == -0.5 * unit)
        {
            heading = -2 * (float)Math.Atan2(q.X, q.W);
            attitude = -(float)Math.PI / 2;
            bank = 0;
        }
        else
        {
            heading = (float)Math.Atan2(2 * (q.Y * q.W - q.X * q.Z), sqx - sqy - sqz + sqw);
            attitude = (float)Math.Asin(2 * (q.X * q.Y + q.Z * q.W));
            bank = (float)Math.Atan2(2 * (q.X * q.W - q.Y * q.Z), -sqx + sqy - sqz + sqw);
        }

        // Convert the Euler angles from radians to degrees
        return new Vector3(heading * 180 / (float)Math.PI, attitude * 180 / (float)Math.PI, bank * 180 / (float)Math.PI);
    }
}