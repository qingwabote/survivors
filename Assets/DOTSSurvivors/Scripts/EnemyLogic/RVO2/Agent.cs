/*
 * Agent.cs
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace RVO
{
    /**
     * <summary>Defines an agent in the simulation.</summary>
     */
    internal struct Agent
    {
        internal static float det(float2 vector1, float2 vector2)
        {
            return vector1.x * vector2.y - vector1.y * vector2.x;
        }

        internal const float RVO_EPSILON = 0.00001f;

        internal int id_;
        internal float2 position_;
        internal float2 prefVelocity_;
        internal float2 velocity_;
        internal int maxNeighbors_;
        internal float maxSpeed_;
        internal float neighborDist_;
        internal float radius_;
        internal float timeHorizon_;
        internal float timeHorizonObst_;

        internal UnsafeList<KeyValuePair<float, int>> agentNeighbors_;
        // internal IList<KeyValuePair<float, Obstacle>> obstacleNeighbors_ = new List<KeyValuePair<float, Obstacle>>();
        internal UnsafeList<Line> orcaLines_;

        private float2 newVelocity_;

        internal Agent(int id, float2 position, float2 prefVelocity, int maxNeighbors, float maxSpeed, float neighborDist, float radius, float timeHorizon, float timeHorizonObst)
        {
            id_ = id;
            position_ = position;
            prefVelocity_ = prefVelocity;
            velocity_ = default;
            maxNeighbors_ = maxNeighbors;
            maxSpeed_ = maxSpeed;
            neighborDist_ = neighborDist;
            radius_ = radius;
            timeHorizon_ = timeHorizon;
            timeHorizonObst_ = timeHorizonObst;

            agentNeighbors_ = new(0, Allocator.Temp);
            orcaLines_ = new(0, Allocator.Temp);

            newVelocity_ = default;
        }

        /**
         * <summary>Computes the neighbors of this agent.</summary>
         */
        internal void computeNeighbors(ref Simulator simulator)
        {
            // obstacleNeighbors_.Clear();
            // float rangeSq = RVOMath.sqr(timeHorizonObst_ * maxSpeed_ + radius_);
            // Simulator.Instance.kdTree_.computeObstacleNeighbors(this, rangeSq);

            // agentNeighbors_.Clear();

            if (maxNeighbors_ > 0)
            {
                float rangeSq = math.square(neighborDist_);
                simulator.kdTree_.queryAgentTreeRecursive(ref simulator, id_, ref rangeSq, 0);
            }
        }

        /**
         * <summary>Computes the new velocity of this agent.</summary>
         */
        internal void computeNewVelocity(ref Simulator simulator)
        {
            orcaLines_.Clear();

            // float invTimeHorizonObst = 1.0f / timeHorizonObst_;

            // /* Create obstacle ORCA lines. */
            // for (int i = 0; i < obstacleNeighbors_.Count; ++i)
            // {

            //     Obstacle obstacle1 = obstacleNeighbors_[i].Value;
            //     Obstacle obstacle2 = obstacle1.next_;

            //     Vector2 relativePosition1 = obstacle1.point_ - position_;
            //     Vector2 relativePosition2 = obstacle2.point_ - position_;

            //     /*
            //      * Check if velocity obstacle of obstacle is already taken care
            //      * of by previously constructed obstacle ORCA lines.
            //      */
            //     bool alreadyCovered = false;

            //     for (int j = 0; j < orcaLines_.Count; ++j)
            //     {
            //         if (RVOMath.det(invTimeHorizonObst * relativePosition1 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON && RVOMath.det(invTimeHorizonObst * relativePosition2 - orcaLines_[j].point, orcaLines_[j].direction) - invTimeHorizonObst * radius_ >= -RVOMath.RVO_EPSILON)
            //         {
            //             alreadyCovered = true;

            //             break;
            //         }
            //     }

            //     if (alreadyCovered)
            //     {
            //         continue;
            //     }

            //     /* Not yet covered. Check for collisions. */
            //     float distSq1 = RVOMath.absSq(relativePosition1);
            //     float distSq2 = RVOMath.absSq(relativePosition2);

            //     float radiusSq = RVOMath.sqr(radius_);

            //     Vector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
            //     float s = (-relativePosition1 * obstacleVector) / RVOMath.absSq(obstacleVector);
            //     float distSqLine = RVOMath.absSq(-relativePosition1 - s * obstacleVector);

            //     Line line;

            //     if (s < 0.0f && distSq1 <= radiusSq)
            //     {
            //         /* Collision with left vertex. Ignore if non-convex. */
            //         if (obstacle1.convex_)
            //         {
            //             line.point = new Vector2(0.0f, 0.0f);
            //             line.direction = RVOMath.normalize(new Vector2(-relativePosition1.y(), relativePosition1.x()));
            //             orcaLines_.Add(line);
            //         }

            //         continue;
            //     }
            //     else if (s > 1.0f && distSq2 <= radiusSq)
            //     {
            //         /*
            //          * Collision with right vertex. Ignore if non-convex or if
            //          * it will be taken care of by neighboring obstacle.
            //          */
            //         if (obstacle2.convex_ && RVOMath.det(relativePosition2, obstacle2.direction_) >= 0.0f)
            //         {
            //             line.point = new Vector2(0.0f, 0.0f);
            //             line.direction = RVOMath.normalize(new Vector2(-relativePosition2.y(), relativePosition2.x()));
            //             orcaLines_.Add(line);
            //         }

            //         continue;
            //     }
            //     else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
            //     {
            //         /* Collision with obstacle segment. */
            //         line.point = new Vector2(0.0f, 0.0f);
            //         line.direction = -obstacle1.direction_;
            //         orcaLines_.Add(line);

            //         continue;
            //     }

            //     /*
            //      * No collision. Compute legs. When obliquely viewed, both legs
            //      * can come from a single vertex. Legs extend cut-off line when
            //      * non-convex vertex.
            //      */

            //     Vector2 leftLegDirection, rightLegDirection;

            //     if (s < 0.0f && distSqLine <= radiusSq)
            //     {
            //         /*
            //          * Obstacle viewed obliquely so that left vertex
            //          * defines velocity obstacle.
            //          */
            //         if (!obstacle1.convex_)
            //         {
            //             /* Ignore obstacle. */
            //             continue;
            //         }

            //         obstacle2 = obstacle1;

            //         float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
            //         leftLegDirection = new Vector2(relativePosition1.x() * leg1 - relativePosition1.y() * radius_, relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
            //         rightLegDirection = new Vector2(relativePosition1.x() * leg1 + relativePosition1.y() * radius_, -relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
            //     }
            //     else if (s > 1.0f && distSqLine <= radiusSq)
            //     {
            //         /*
            //          * Obstacle viewed obliquely so that
            //          * right vertex defines velocity obstacle.
            //          */
            //         if (!obstacle2.convex_)
            //         {
            //             /* Ignore obstacle. */
            //             continue;
            //         }

            //         obstacle1 = obstacle2;

            //         float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
            //         leftLegDirection = new Vector2(relativePosition2.x() * leg2 - relativePosition2.y() * radius_, relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
            //         rightLegDirection = new Vector2(relativePosition2.x() * leg2 + relativePosition2.y() * radius_, -relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
            //     }
            //     else
            //     {
            //         /* Usual situation. */
            //         if (obstacle1.convex_)
            //         {
            //             float leg1 = RVOMath.sqrt(distSq1 - radiusSq);
            //             leftLegDirection = new Vector2(relativePosition1.x() * leg1 - relativePosition1.y() * radius_, relativePosition1.x() * radius_ + relativePosition1.y() * leg1) / distSq1;
            //         }
            //         else
            //         {
            //             /* Left vertex non-convex; left leg extends cut-off line. */
            //             leftLegDirection = -obstacle1.direction_;
            //         }

            //         if (obstacle2.convex_)
            //         {
            //             float leg2 = RVOMath.sqrt(distSq2 - radiusSq);
            //             rightLegDirection = new Vector2(relativePosition2.x() * leg2 + relativePosition2.y() * radius_, -relativePosition2.x() * radius_ + relativePosition2.y() * leg2) / distSq2;
            //         }
            //         else
            //         {
            //             /* Right vertex non-convex; right leg extends cut-off line. */
            //             rightLegDirection = obstacle1.direction_;
            //         }
            //     }

            //     /*
            //      * Legs can never point into neighboring edge when convex
            //      * vertex, take cutoff-line of neighboring edge instead. If
            //      * velocity projected on "foreign" leg, no constraint is added.
            //      */

            //     Obstacle leftNeighbor = obstacle1.previous_;

            //     bool isLeftLegForeign = false;
            //     bool isRightLegForeign = false;

            //     if (obstacle1.convex_ && RVOMath.det(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
            //     {
            //         /* Left leg points into obstacle. */
            //         leftLegDirection = -leftNeighbor.direction_;
            //         isLeftLegForeign = true;
            //     }

            //     if (obstacle2.convex_ && RVOMath.det(rightLegDirection, obstacle2.direction_) <= 0.0f)
            //     {
            //         /* Right leg points into obstacle. */
            //         rightLegDirection = obstacle2.direction_;
            //         isRightLegForeign = true;
            //     }

            //     /* Compute cut-off centers. */
            //     Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
            //     Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
            //     Vector2 cutOffVector = rightCutOff - leftCutOff;

            //     /* Project current velocity on velocity obstacle. */

            //     /* Check if current velocity is projected on cutoff circles. */
            //     float t = obstacle1 == obstacle2 ? 0.5f : ((velocity_ - leftCutOff) * cutOffVector) / RVOMath.absSq(cutOffVector);
            //     float tLeft = (velocity_ - leftCutOff) * leftLegDirection;
            //     float tRight = (velocity_ - rightCutOff) * rightLegDirection;

            //     if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
            //     {
            //         /* Project on left cut-off circle. */
            //         Vector2 unitW = RVOMath.normalize(velocity_ - leftCutOff);

            //         line.direction = new Vector2(unitW.y(), -unitW.x());
            //         line.point = leftCutOff + radius_ * invTimeHorizonObst * unitW;
            //         orcaLines_.Add(line);

            //         continue;
            //     }
            //     else if (t > 1.0f && tRight < 0.0f)
            //     {
            //         /* Project on right cut-off circle. */
            //         Vector2 unitW = RVOMath.normalize(velocity_ - rightCutOff);

            //         line.direction = new Vector2(unitW.y(), -unitW.x());
            //         line.point = rightCutOff + radius_ * invTimeHorizonObst * unitW;
            //         orcaLines_.Add(line);

            //         continue;
            //     }

            //     /*
            //      * Project on left leg, right leg, or cut-off line, whichever is
            //      * closest to velocity.
            //      */
            //     float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + t * cutOffVector));
            //     float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (leftCutOff + tLeft * leftLegDirection));
            //     float distSqRight = tRight < 0.0f ? float.PositiveInfinity : RVOMath.absSq(velocity_ - (rightCutOff + tRight * rightLegDirection));

            //     if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
            //     {
            //         /* Project on cut-off line. */
            //         line.direction = -obstacle1.direction_;
            //         line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
            //         orcaLines_.Add(line);

            //         continue;
            //     }

            //     if (distSqLeft <= distSqRight)
            //     {
            //         /* Project on left leg. */
            //         if (isLeftLegForeign)
            //         {
            //             continue;
            //         }

            //         line.direction = leftLegDirection;
            //         line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
            //         orcaLines_.Add(line);

            //         continue;
            //     }

            //     /* Project on right leg. */
            //     if (isRightLegForeign)
            //     {
            //         continue;
            //     }

            //     line.direction = -rightLegDirection;
            //     line.point = rightCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y(), line.direction.x());
            //     orcaLines_.Add(line);
            // }

            int numObstLines = orcaLines_.Length;

            float invTimeHorizon = 1.0f / timeHorizon_;

            /* Create agent ORCA lines. */
            for (int i = 0; i < agentNeighbors_.Length; ++i)
            {
                ref var other = ref simulator.AgentAt(agentNeighbors_[i].Value);

                float2 relativePosition = other.position_ - position_;
                float2 relativeVelocity = velocity_ - other.velocity_;
                float distSq = math.lengthsq(relativePosition);
                float combinedRadius = radius_ + other.radius_;
                float combinedRadiusSq = math.square(combinedRadius);

                Line line;
                float2 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    float2 w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = math.lengthsq(w);
                    float dotProduct1 = math.dot(w, relativePosition);

                    if (dotProduct1 < 0.0f && math.square(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = math.sqrt(wLengthSq);
                        float2 unitW = w / wLength;

                        line.direction = new float2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = math.sqrt(distSq - combinedRadiusSq);

                        if (det(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.direction = new float2(relativePosition.x * leg - relativePosition.y * combinedRadius, relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new float2(relativePosition.x * leg + relativePosition.y * combinedRadius, -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        float dotProduct2 = math.dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    float invTimeStep = 1.0f / simulator.timeStep_;

                    /* Vector from cutoff center to relative velocity. */
                    float2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = math.length(w);
                    float2 unitW = w / wLength;

                    line.direction = new float2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocity_ + 0.5f * u;
                orcaLines_.Add(line);
            }

            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);

            if (lineFail < orcaLines_.Length)
            {
                linearProgram3(orcaLines_, numObstLines, lineFail, maxSpeed_, ref newVelocity_);
            }
        }

        /**
         * <summary>Inserts an agent neighbor into the set of neighbors of this
         * agent.</summary>
         *
         * <param name="agent">A pointer to the agent to be inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        internal void insertAgentNeighbor(ref Simulator simulator, int agent, ref float rangeSq)
        {
            if (id_ != agent)
            {
                float distSq = math.lengthsq(position_ - simulator.AgentAt(agent).position_);

                if (distSq < rangeSq)
                {
                    if (agentNeighbors_.Length < maxNeighbors_)
                    {
                        agentNeighbors_.Add(new KeyValuePair<float, int>(distSq, agent));
                    }

                    int i = agentNeighbors_.Length - 1;

                    while (i != 0 && distSq < agentNeighbors_[i - 1].Key)
                    {
                        agentNeighbors_[i] = agentNeighbors_[i - 1];
                        --i;
                    }

                    agentNeighbors_[i] = new KeyValuePair<float, int>(distSq, agent);

                    if (agentNeighbors_.Length == maxNeighbors_)
                    {
                        rangeSq = agentNeighbors_[agentNeighbors_.Length - 1].Key;
                    }
                }
            }
        }

        /**
         * <summary>Inserts a static obstacle neighbor into the set of neighbors
         * of this agent.</summary>
         *
         * <param name="obstacle">The number of the static obstacle to be
         * inserted.</param>
         * <param name="rangeSq">The squared range around this agent.</param>
         */
        // internal void insertObstacleNeighbor(Obstacle obstacle, float rangeSq)
        // {
        //     Obstacle nextObstacle = obstacle.next_;

        //     float distSq = RVOMath.distSqPointLineSegment(obstacle.point_, nextObstacle.point_, position_);

        //     if (distSq < rangeSq)
        //     {
        //         obstacleNeighbors_.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

        //         int i = obstacleNeighbors_.Count - 1;

        //         while (i != 0 && distSq < obstacleNeighbors_[i - 1].Key)
        //         {
        //             obstacleNeighbors_[i] = obstacleNeighbors_[i - 1];
        //             --i;
        //         }
        //         obstacleNeighbors_[i] = new KeyValuePair<float, Obstacle>(distSq, obstacle);
        //     }
        // }

        /**
         * <summary>Updates the two-dimensional position and two-dimensional
         * velocity of this agent.</summary>
         */
        internal void update(float timeStep)
        {
            velocity_ = newVelocity_;
            position_ += velocity_ * timeStep;
        }

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private bool linearProgram1(UnsafeList<Line> lines, int lineNo, float radius, float2 optVelocity, bool directionOpt, ref float2 result)
        {
            float dotProduct = math.dot(lines[lineNo].point, lines[lineNo].direction);
            float discriminant = math.square(dotProduct) + math.square(radius) - math.lengthsq(lines[lineNo].point);

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = math.sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = det(lines[lineNo].direction, lines[i].direction);
                float numerator = det(lines[i].direction, lines[lineNo].point - lines[i].point);

                if (math.abs(denominator) <= RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Math.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Math.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (math.dot(optVelocity, lines[lineNo].direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = math.dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private int linearProgram2(UnsafeList<Line> lines, float radius, float2 optVelocity, bool directionOpt, ref float2 result)
        {
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                result = optVelocity * radius;
            }
            else if (math.lengthsq(optVelocity) > math.square(radius))
            {
                /* Optimize closest point and outside circle. */
                result = math.normalize(optVelocity) * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < lines.Length; ++i)
            {
                if (det(lines[i].direction, lines[i].point - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    float2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return lines.Length;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="numObstLines">Count of obstacle lines.</param>
         * <param name="beginLine">The line on which the 2-d linear program
         * failed.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private void linearProgram3(UnsafeList<Line> lines, int numObstLines, int beginLine, float radius, ref float2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Length; ++i)
            {
                if (det(lines[i].direction, lines[i].point - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    UnsafeList<Line> projLines = new(0, Allocator.Temp);
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        float determinant = det(lines[i].direction, lines[j].direction);

                        if (math.abs(determinant) <= RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            if (math.dot(lines[i].direction, lines[j].direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = math.normalize(lines[j].direction - lines[i].direction);
                        projLines.Add(line);
                    }

                    float2 tempResult = result;
                    if (linearProgram2(projLines, radius, new float2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Length)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = det(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}
