import React from 'react';
import { Card } from 'antd';
import ProjectsManager from '../components/Projects/ProjectsManager';

export default function ProjectsPage(): React.ReactElement {
  return (
    <Card>
      <ProjectsManager />
    </Card>
  );
}
